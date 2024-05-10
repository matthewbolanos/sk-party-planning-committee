namespace SpeakerService.Models
{
    /// <summary>
    /// Request model to change the state of a speaker.
    /// </summary>
    public class ChangeSpeakerStateRequest
    {
        /// <summary>
        /// Gets or sets whether the speaker is playing.
        /// True for playing, false for paused, or null for no change.
        /// </summary>
        public bool? IsPlaying { get; set; }

        /// <summary>
        /// Gets or sets the current song being played on the speaker.
        /// Set to null for no change.
        /// </summary>
        public string? CurrentSong { get; set; }

        /// <summary>
        /// Gets or sets the volume of the speaker.
        /// Values range from 0 (mute) to 100 (maximum volume).
        /// Set to null for no change.
        /// </summary>
        public byte? Volume { get; set; }

        /// <summary>
        /// The time at which the change should occur.
        /// </summary>
        public DateTime ScheduledTime { get; set; } = DateTime.Now;
    }
}
