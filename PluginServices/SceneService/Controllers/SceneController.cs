using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel.TextToImage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Microsoft.SemanticKernel.Memory;
using System.Text.Json;
using Microsoft.SemanticKernel.Embeddings;
using System.ComponentModel.DataAnnotations;

#pragma warning disable SKEXP0001
namespace SceneService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SceneController(
        ITextToImageService textToImageService,
        IMemoryStore memoryStore,
        ITextEmbeddingGenerationService textEmbeddingGenerationService
    ) : ControllerBase
    {
        /// <summary>
        /// Generates a light color palette for a scene based on a description; _must_ use when setting the color of lights.
        /// </summary>
        /// <param name="sceneRequest">The details about the scene to generate</param>
        /// <returns>Returns a list of colors for a scene</returns>
        [HttpPost(Name="generate_scene_pallette")]
        [ProducesResponseType(typeof(ScenePallette), StatusCodes.Status200OK)]
        public async Task<IActionResult> GenerateScenePallette([FromBody, Required] SceneRequest sceneRequest)
        {
            string completePrompt = $"{sceneRequest.ThreeWordDescription}{(string.IsNullOrEmpty(sceneRequest.RecommendedColors) ?"" : $"; {sceneRequest.RecommendedColors}")}";

            // Check cache for scene
            try
            {
                var matches = await memoryStore.GetNearestMatchAsync(
                    collectionName: "scene",
                    embedding: await textEmbeddingGenerationService.GenerateEmbeddingAsync(completePrompt),
                    minRelevanceScore: 1
                );
                var distance = matches.Value.Item2;

                if (distance < 0.3)
                {
                    // Return the scene
                    var metadata = matches.Value.Item1.Metadata;
                    return Ok(
                        new ScenePallette(
                            metadata.AdditionalMetadata,
                            JsonSerializer.Deserialize<List<string>>(metadata.Description)!
                        )
                    );
                }
            }
            catch (Exception) {}

            // Generate an image
            string imageUrl = await textToImageService.GenerateImageAsync($"""
                Realistic image for desktop background: {completePrompt}
                """, 256, 256);

            string localImagePath = await DownloadImageAsync(imageUrl);
            List<string> hexColors = GetTopColors(localImagePath, 5);

            // Cache the scene
            await memoryStore.UpsertAsync(
                "scene",
                new MemoryRecord(
                    metadata: new MemoryRecordMetadata(
                        isReference: false,
                        id: Guid.NewGuid().ToString(),
                        text: sceneRequest.ThreeWordDescription,
                        description: JsonSerializer.Serialize(hexColors),
                        externalSourceName: string.Empty,
                        additionalMetadata: imageUrl
                    ),
                    embedding: await textEmbeddingGenerationService.GenerateEmbeddingAsync(completePrompt),
                    null
                )
            );

            return Ok(new ScenePallette(imageUrl, hexColors));
        }

        static async Task<string> DownloadImageAsync(string url)
        {
            using HttpClient client = new();
            byte[] imageBytes = await client.GetByteArrayAsync(url);
            string outputPath = Path.GetTempFileName();
            await System.IO.File.WriteAllBytesAsync(outputPath, imageBytes);

            return outputPath;
        }

        static List<string> GetTopColors(string imagePath, int colorCount)
        {
            using var image = Image.Load<Rgba32>(imagePath);
            var colorThief = new ColorThief.ImageSharp.ColorThief();
            var palette = colorThief.GetPalette(image, colorCount);

            return palette.Select(c => c.Color.ToHexString()).ToList();
        }
    }

    public class SceneRequest
    {
        /// <summary>
        /// The palette to generate in 1-3 sentence (feel free to be creative!)
        /// </summary>
        [Required]
        public string ThreeWordDescription { get; set; }

        /// <summary>
        /// The name of 3 recommended colors for the scene based on your expertise (no need to ask the user; just do it!)
        /// </summary>
        [Required]
        public string RecommendedColors { get; set; }
    }
}
#pragma warning restore SKEXP0001
