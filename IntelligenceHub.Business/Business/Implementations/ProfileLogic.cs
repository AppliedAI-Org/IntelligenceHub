using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.Business.Handlers;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Common.Config;
using IntelligenceHub.DAL;
using IntelligenceHub.DAL.Interfaces;
using Microsoft.Extensions.Options;

namespace IntelligenceHub.Business.Implementations
{
    /// <summary>
    /// Business logic for handling profile operations.
    /// </summary>
    public class ProfileLogic : IProfileLogic
    {
        private readonly IProfileRepository _profileDb;
        private readonly IProfileToolsAssociativeRepository _profileToolsDb;
        private readonly IToolRepository _toolDb;
        private readonly IPropertyRepository _propertyDb;
        private readonly IValidationHandler _validationLogic;

        private readonly string _defaulAzureModel;

        private readonly string _unknownErroMessage = "something went wrong...";
        private readonly string _missingProfileMessage = "No profile with the specified name was found. Name: "; // make sure to use interpolation here
        private readonly string _missingToolMessage = "No tool with the specified name was found. Name: "; // make sure to use interpolation here

        /// <summary>
        /// Constructor for ProfileLogic resolved with DI.
        /// </summary>
        /// <param name="profileDb">The repository used to retrieve profile data.</param>
        /// <param name="profileToolsDb">The repository used to retrieve associations between profiles and tools.</param>
        /// <param name="toolDb">The repository used to retrieve tool data.</param>
        /// <param name="propertyDb">The repository used to retrieve property data, which are associated with tools.</param>
        /// <param name="validationLogic">The validation class used to assess the validity of request properties.</param>
        public ProfileLogic(IOptionsMonitor<Settings> settings, IProfileRepository profileDb, IProfileToolsAssociativeRepository profileToolsDb, IToolRepository toolDb, IPropertyRepository propertyDb, IValidationHandler validationLogic)
        {
            _defaulAzureModel = settings.CurrentValue.ValidAGIModels.FirstOrDefault() ?? string.Empty;
            _profileDb = profileDb;
            _profileToolsDb = profileToolsDb;
            _toolDb = toolDb;
            _propertyDb = propertyDb;
            _validationLogic = validationLogic;
        }

        /// <summary>
        /// Retrieves a profile by name.
        /// </summary>
        /// <param name="name">The name of the profile.</param>
        /// <returns>The profile, if one with the requested name exists, otherwise null.</returns>
        public async Task<Profile?> GetProfile(string name) // else shouldn't be required here
        {
            var dbProfile = await _profileDb.GetByNameAsync(name);
            if (dbProfile != null)
            {
                var profile = DbMappingHandler.MapFromDbProfile(dbProfile);
                // package this into a seperate method (same one as in GetAllProfiles())
                var profileToolDTOs = await _profileToolsDb.GetToolAssociationsAsync(dbProfile.Id);
                profile.Tools = new List<Tool>();
                foreach (var association in profileToolDTOs)
                {
                    var dbTool = await _toolDb.GetByIdAsync(association.ToolID);
                    if (dbTool == null) continue;
                    var mappedTool = DbMappingHandler.MapFromDbTool(dbTool);
                    profile.Tools.Add(mappedTool);
                }
                return profile;
            }
            else
            {
                var apiProfile = await _profileDb.GetByNameAsync(name);
                if (apiProfile != null) return DbMappingHandler.MapFromDbProfile(apiProfile);
            }
            return null;
        }

        /// <summary>
        /// Retrieves all profiles.
        /// </summary>
        /// <param name="page">The page number to retrieve.</param>
        /// <param name="count">The amount of profiles to retrieve.</param>
        /// <returns>An list of all existing profiles.</returns>
        public async Task<IEnumerable<Profile>> GetAllProfiles(int page, int count)
        {
            var response = await _profileDb.GetAllAsync(page, count);
            var apiResponseList = new List<Profile>();
            if (response != null)
            {
                foreach (var profile in response)
                {
                    // package this into a seperate method (same one as in GetProfiles())
                    var apiProfileDto = DbMappingHandler.MapFromDbProfile(profile);
                    var profileToolDTOs = await _profileToolsDb.GetToolAssociationsAsync(profile.Id);
                    apiProfileDto.Tools = new List<Tool>();
                    foreach (var association in profileToolDTOs)
                    {
                        var tool = await _toolDb.GetByIdAsync(association.ToolID);
                        if (tool == null) continue;
                        var mappedTool = DbMappingHandler.MapFromDbTool(tool);
                        apiProfileDto.Tools.Add(mappedTool);
                    }
                    apiResponseList.Add(apiProfileDto);
                }
            }
            return apiResponseList;
        }

        /// <summary>
        /// Creates or updates an AGI client profile.
        /// </summary>
        /// <param name="profileDto">The request body used to create or update the profile.</param>
        /// <returns>An error message if the profile DTO fails to pass validation, otherwise null.</returns>
        public async Task<string?> CreateOrUpdateProfile(Profile profileDto) // refactor this
        {
            var errorMessage = _validationLogic.ValidateAPIProfile(profileDto); // move to controller
            if (errorMessage != null) return errorMessage;
            var existingProfile = await _profileDb.GetByNameAsync(profileDto.Name);
            var success = true;
            if (existingProfile != null)
            {
                var updateProfileDto = DbMappingHandler.MapToDbProfile(existingProfile.Name, _defaulAzureModel, existingProfile, profileDto);
                var rows = await _profileDb.UpdateAsync(updateProfileDto);
                if (rows != 1) success = false;
            }
            else
            {
                var updateProfileDto = DbMappingHandler.MapToDbProfile(profileDto.Name, _defaulAzureModel, null, profileDto);
                var newTool = await _profileDb.AddAsync(updateProfileDto);
                if (newTool == null) success = false;
            }

            if (profileDto.Tools != null && profileDto.Tools.Count > 0)
            {
                if (existingProfile == null) success = await AddOrUpdateProfileTools(profileDto, null);
                else success = await AddOrUpdateProfileTools(profileDto, DbMappingHandler.MapFromDbProfile(existingProfile));
            }
            if (!success) return _unknownErroMessage;
            return null;
        }

        /// <summary>
        /// Deletes an AGI client profile by name.
        /// </summary>
        /// <param name="name">The name of the profile to delete.</param>
        /// <returns>An error message if the operation faile, otherwise null.</returns>
        public async Task<string?> DeleteProfile(string name) // could also use some refactoring
        {
            var dbProfile = await _profileDb.GetByNameAsync(name);
            int rows;
            if (dbProfile != null)
            {
                var tools = new List<Tool>();
                var dbTools = dbProfile.ProfileTools.Select(pt => pt.Tool).ToList();
                foreach (var tool in dbTools) tools.Add(DbMappingHandler.MapFromDbTool(tool));
                var profile = DbMappingHandler.MapFromDbProfile(dbProfile, tools);

                if (profile.Tools != null && profile.Tools.Count > 0) await _profileToolsDb.DeleteAllProfileAssociationsAsync(dbProfile.Id);

                rows = await _profileDb.DeleteAsync(dbProfile);
            }
            else
            {
                var toolLessProfile = await _profileDb.GetByNameAsync(name);
                if (toolLessProfile == null) return _missingProfileMessage + $"'{name}'";
                rows = await _profileDb.DeleteAsync(toolLessProfile);
            }
            if (rows != 1) return _unknownErroMessage;
            return null;
        }

        #region Tool Logic

        /// <summary>
        /// Adds or updates the tools associated with a profile.
        /// </summary>
        /// <param name="profileDto">The DTO used to construct the profile.</param>
        /// <param name="existingProfile">The existing profile in the database.</param>
        /// <returns>A bool indicating the operations success.</returns>
        private async Task<bool> AddOrUpdateProfileTools(Profile profileDto, Profile existingProfile)
        {
            var toolIds = new List<int>();
            await _profileToolsDb.DeleteAllProfileAssociationsAsync(existingProfile.Id);
            if (profileDto.Tools != null && profileDto.Tools.Count > 0)
            {
                await CreateOrUpdateTools(profileDto.Tools);
                foreach (var tool in profileDto.Tools)
                {
                    var dbTool = await _toolDb.GetByNameAsync(tool.Function.Name);
                    toolIds.Add(dbTool.Id);
                }
                await _profileToolsDb.AddAssociationsByProfileIdAsync(existingProfile.Id, toolIds);
            }
            return true;
        }

        /// <summary>
        /// Retrieves a tool by name.
        /// </summary>
        /// <param name="name">The name of the tool.</param>
        /// <returns>The tool if it exists in the database.</returns>
        public async Task<Tool?> GetTool(string name)
        {
            var dbTool = await _toolDb.GetByNameAsync(name);
            if (dbTool == null) return null;

            return DbMappingHandler.MapFromDbTool(dbTool);
        }

        /// <summary>
        /// Retrieves all tools in the database.
        /// </summary>
        /// <param name="page">The page number to retrieve.</param>
        /// <param name="count">The amount of tools to retrieve.</param>
        /// <returns>A list of all existing tools.</returns>
        public async Task<IEnumerable<Tool>> GetAllTools(int page, int count)
        {
            var returnList = new List<Tool>();
            var dbTools = await _toolDb.GetAllAsync(page, count);
            foreach (var dbTool in dbTools)
            {
                var properties = await _propertyDb.GetToolProperties(dbTool.Id);
                var tool = DbMappingHandler.MapFromDbTool(dbTool, properties.ToList());
                returnList.Add(tool);
            }
            return returnList;
        }

        /// <summary>
        /// Retrieves the tools associated with a profile.
        /// </summary>
        /// <param name="name">The name of the profile.</param>
        /// <returns>A list of tool names.</returns>
        public async Task<List<string>> GetProfileToolAssociations(string name)
        {
            var profileNames = await _toolDb.GetProfileToolsAsync(name);
            if (profileNames.Count > 0) return profileNames;
            else return new List<string>();
        }

        /// <summary>
        /// Retrieves the profiles associated with a tool.
        /// </summary>
        /// <param name="name">The name of the tools.</param>
        /// <returns>A list of profile names.</returns>
        public async Task<List<string>> GetToolProfileAssociations(string name)
        {
            var profileNames = await _toolDb.GetToolProfilesAsync(name);
            if (profileNames.Count > 0) return profileNames;
            else return new List<string>();
        }

        /// <summary>
        /// Creates or updates a list of tools.
        /// </summary>
        /// <param name="toolList">A list of tools to create or update.</param>
        /// <returns>An error message if the operation failed, otherwise null.</returns>
        public async Task<string?> CreateOrUpdateTools(List<Tool> toolList)
        {
            // move below to controller
            foreach (var tool in toolList)
            {
                var errorMessage = _validationLogic.ValidateTool(tool);
                if (errorMessage != null) return errorMessage;
            }
            foreach (var tool in toolList)
            {
                var dbToolDTO = DbMappingHandler.MapToDbTool(tool);
                var existingDbTool = await _toolDb.GetByNameAsync(tool.Function.Name);
                var existingToolDTO = DbMappingHandler.MapFromDbTool(existingDbTool);
                if (existingToolDTO != null)
                {
                    var existingTool = DbMappingHandler.MapToDbTool(existingToolDTO);
                    await _toolDb.UpdateAsync(dbToolDTO);
                    await AddOrUpdateToolProperties(existingToolDTO, tool.Function.Parameters.properties);
                }
                else
                {
                    var newDbTool = await _toolDb.AddAsync(dbToolDTO);
                    var newTool = DbMappingHandler.MapFromDbTool(newDbTool);
                    await AddOrUpdateToolProperties(newTool, tool.Function.Parameters.properties);
                }
            }
            return null;
        }

        /// <summary>
        /// Associates a list of profiles with a tool.
        /// </summary>
        /// <param name="name">The name of the tool.</param>
        /// <param name="profiles">A list of profile names.</param>
        /// <returns>An error message if the operation fails, otherwise null.</returns>
        public async Task<string?> AddToolToProfiles(string name, List<string> profiles)
        {
            var tool = await _toolDb.GetByNameAsync(name);
            if (tool != null)
            {
                var success = await _profileToolsDb.AddAssociationsByToolIdAsync(tool.Id, profiles);
                if (success) return null;
                else return _unknownErroMessage;
            }
            return _missingToolMessage + $"'{name}'";
        }

        /// <summary>
        /// Associates a list of tools with a profile.
        /// </summary>
        /// <param name="name">The name of the profile.</param>
        /// <param name="tools">A list of tool names.</param>
        /// <returns>An error message if the operation fails, otherwise null.</returns>
        public async Task<string?> AddProfileToTools(string name, List<string> tools)
        {
            var toolIDs = new List<int>();
            foreach (var toolName in tools)
            {
                var tool = await _toolDb.GetByNameAsync(toolName);
                if (tool is null) return _missingToolMessage + $"'{toolName}'"; ;
                toolIDs.Add(tool.Id);
            }
            var profile = await _profileDb.GetByNameAsync(name);
            if (profile != null && toolIDs.Count > 0)
            {
                var success = await _profileToolsDb.AddAssociationsByProfileIdAsync(profile.Id, toolIDs);
                if (success) return null;
            }
            return _missingProfileMessage + $"'{name}'";
        }

        /// <summary>
        /// Deletes the associations between a tool and a list of profiles.
        /// </summary>
        /// <param name="name">The name of the tool.</param>
        /// <param name="profiles">A list of profile names.</param>
        /// <returns>An error message if the operation fails, otherwise null.</returns>
        public async Task<string?> DeleteToolAssociations(string name, List<string> profiles)
        {
            var tool = await _toolDb.GetByNameAsync(name);
            if (tool != null)
            {
                foreach (var profile in profiles) await _profileToolsDb.DeleteToolAssociationAsync(tool.Id, profile);
                return null;
            }
            return _missingToolMessage + $"'{name}'";
        }

        /// <summary>
        /// Deletes the associations between a profile and a list of tools.
        /// </summary>
        /// <param name="name">The name of the profile.</param>
        /// <param name="tools">A list of tool names.</param>
        /// <returns>An error message if the operation fails, otherwise null.</returns>
        public async Task<string?> DeleteProfileAssociations(string name, List<string> tools)
        {
            var profile = await _profileDb.GetByNameAsync(name);
            if (profile != null)
            {
                foreach (var tool in tools) await _profileToolsDb.DeleteProfileAssociationAsync(profile.Id, tool);
                return null;
            }
            return _missingToolMessage + $"'{name}'";
        }

        /// <summary>
        /// Deletes a tool by name.
        /// </summary>
        /// <param name="name">The name of the tool.</param>
        /// <returns>A bool indicating the success of the operation.</returns>
        public async Task<bool> DeleteTool(string name)
        {
            var existingDbTool = await _toolDb.GetByNameAsync(name);
            var dbProperites = await _propertyDb.GetToolProperties(existingDbTool.Id);
            var existingTool = DbMappingHandler.MapFromDbTool(existingDbTool, dbProperites.ToList());
            if (existingTool != null)
            {
                foreach (var property in existingTool.Function.Parameters.properties)
                {
                    var propertyDTO = DbMappingHandler.MapToDbProperty(property.Key, property.Value);
                    await _propertyDb.DeleteAsync(propertyDTO);
                }
                await _profileToolsDb.DeleteAllToolAssociationsAsync(existingTool.Id);
                var dbTool = DbMappingHandler.MapToDbTool(existingTool);
                return await _toolDb.DeleteAsync(dbTool) == 1;
            }
            return false;
        }

        /// <summary>
        /// Adds or updates the properties associated with a tool.
        /// </summary>
        /// <param name="existingTool">The existing tool.</param>
        /// <param name="newProperties">A dictionary of properties where the name of the 
        /// property is the key, and the value is the property object.</param>
        /// <returns>A bool indicating the success of the operation.</returns>
        public async Task<bool> AddOrUpdateToolProperties(Tool existingTool, Dictionary<string, Property> newProperties)
        {
            var existingProperties = await _propertyDb.GetToolProperties(existingTool.Id);
            foreach (var property in existingProperties) await _propertyDb.DeleteAsync(property);
            foreach (var property in newProperties)
            {
                property.Value.Id = existingTool.Id;
                await _propertyDb.AddAsync(DbMappingHandler.MapToDbProperty(property.Key, property.Value));
            }
            return true;
        }
        #endregion
    }
}
