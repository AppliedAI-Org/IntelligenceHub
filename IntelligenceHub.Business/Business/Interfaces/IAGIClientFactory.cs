using IntelligenceHub.Client.Interfaces;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Business.Interfaces
{
    public interface IAGIClientFactory
    {
        IAGIClient GetClient(AGIServiceHosts? modelName);
    }
}
