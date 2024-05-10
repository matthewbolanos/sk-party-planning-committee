using Shared.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using SpeakerService.Models;

namespace SpeakerService.Controllers
{
    /// <summary>
    /// API Controller for managing speakers.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class SpeakersController(IMongoDatabase database) : ControllerBase
    {
        private readonly IMongoCollection<BsonDocument> _smartDevices = database.GetCollection<BsonDocument>("SmartDevices");
        private readonly IMongoCollection<Speaker> _speakers = database.GetCollection<Speaker>("Speakers");
        private const int LatencyBuffer = 300; // milliseconds

        /// <summary>
        /// Retrieves all speakers.
        /// </summary>
        /// <returns>A list of all speakers.</returns>
        [HttpGet(Name = "get_all_speakers")]
        public IActionResult GetSpeakers()
        {
            var smartDevices = _smartDevices
                .Find(new BsonDocument { { "Type", "Speaker" } })
                .ToList();

            var deviceIds = smartDevices.Select(doc => doc["_id"].AsString).ToList();

            var speakers = _speakers
                .Find(Builders<Speaker>.Filter.In(d => d.Id, deviceIds))
                .ToList();

            var combinedSpeakers = speakers.Select(speaker =>
            {
                var smartDevice = smartDevices.FirstOrDefault(sd => sd["_id"] == speaker.Id);
                if (smartDevice != null)
                {
                    speaker.Name = smartDevice["Name"].AsString;
                    speaker.Type = smartDevice["Type"].AsString;
                }
                return speaker;
            }).ToList();

            return Ok(combinedSpeakers);
        }

        /// <summary>
        /// Retrieves a specific speaker by its ID.
        /// </summary>
        /// <param name="id">The ID of the speaker to retrieve.</param>
        /// <returns>The requested speaker or a 404 error if not found.</returns>
        [HttpGet("{id}", Name = "get_speaker")]
        public IActionResult GetSpeaker(string id)
        {
            var smartDevice = _smartDevices
                .Find(Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("_id", id),
                    Builders<BsonDocument>.Filter.Eq("Type", "Speaker")))
                .FirstOrDefault();

            if (smartDevice == null)
            {
                return NotFound();
            }

            var speaker = _speakers.Find(d => d.Id == id).FirstOrDefault();
            if (speaker == null)
            {
                return NotFound();
            }

            speaker.Name = smartDevice["Name"].AsString;
            speaker.Type = smartDevice["Type"].AsString;

            return Ok(speaker);
        }

        /// <summary>
        /// Changes the state of a specific speaker.
        /// </summary>
        /// <param name="id">The ID of the speaker to change.</param>
        /// <param name="newStateRequest">The new state request for the speaker.</param>
        /// <returns>The updated speaker or a 404 error if not found.</returns>
        [HttpPost("{id}", Name = "change_speaker_state")]
        public IActionResult ChangeSpeakerState(string id, ChangeSpeakerStateRequest newStateRequest)
        {
            var smartDevice = _smartDevices
                .Find(Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("_id", id),
                    Builders<BsonDocument>.Filter.Eq("Type", "Speaker")))
                .FirstOrDefault();

            if (smartDevice == null)
            {
                return NotFound();
            }

            var speaker = _speakers.Find(d => d.Id == id).FirstOrDefault();
            if (speaker == null)
            {
                return NotFound();
            }

            DateTime scheduledTime = newStateRequest.ScheduledTime.AddMilliseconds(LatencyBuffer);
            if (scheduledTime > DateTime.Now)
            {
                var timer = new Timer(
                    _ => speaker.ChangeState(
                        newStateRequest.IsPlaying,
                        newStateRequest.CurrentSong,
                        newStateRequest.Volume),
                    null,
                    scheduledTime - DateTime.Now,
                    TimeSpan.FromMilliseconds(-1));
            }
            else
            {
                speaker.ChangeState(
                    newStateRequest.IsPlaying,
                    newStateRequest.CurrentSong,
                    newStateRequest.Volume);
            }

            _speakers.ReplaceOne(d => d.Id == speaker.Id, speaker);
            return Ok(speaker);
        }
    }
}
