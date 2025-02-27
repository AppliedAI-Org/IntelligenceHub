using IntelligenceHub.API.DTOs;

namespace IntelligenceHub.Client.Interfaces
{
    /// <summary>
    /// Interface for an AGI client. Can be utilized to easily add additional AGI services to the Intelligence Hub via polymorphism.
    /// </summary>
    public interface IAGIClient
    {
        /// <summary>
        /// Generates an image based on the provided prompt.
        /// </summary>
        /// <param name="prompt">The prompt used to generate the image.</param>
        /// <returns>A base 64 representation of the image.</returns>
        public Task<string?> GenerateImage(string prompt);

        /// <summary>
        /// Posts a completion request to an AGI client and returns the completion response.
        /// </summary>
        /// <param name="completionRequest">The CompletionRequest request details used to generate a completion.</param>
        /// <returns>The completion response.</returns>
        public Task<CompletionResponse> PostCompletion(CompletionRequest completionRequest);

        /// <summary>
        /// Streams a completion request to an AGI client and returns the completion response.
        /// </summary>
        /// <param name="completionRequest">The CompletionRequest request details used to generate a completion.</param>
        /// <returns>An asyncronous collection of CompletionStreamChunks.</returns>
        public IAsyncEnumerable<CompletionStreamChunk> StreamCompletion(CompletionRequest completionRequest);
    }
}
