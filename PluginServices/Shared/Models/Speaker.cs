namespace Shared.Models
{
    /// <summary>
    /// Represents a speaker with various properties.
    /// </summary>
    public class Speaker(string name) : SmartDevice(name, "Speaker")
    {
        /// <summary>
        /// Specifies whether the speaker is currently playing audio.
        /// </summary>
        public bool IsPlaying { get; private set; } = false;

        /// <summary>
        /// The name of the currently playing song.
        /// </summary>
        public string CurrentSong { get; private set; } = "No Song";

        /// <summary>
        /// The volume level of the speaker (0-255).
        /// </summary>
        public byte Volume { get; private set; } = 100;

        /// <summary>
        /// Changes the state of the speaker.
        /// </summary>
        /// <param name="isPlaying">Specifies whether the speaker is currently playing audio.</param>
        /// <param name="currentSong">The name of the currently playing song.</param>
        /// <param name="volume">The volume level of the speaker.</param>
        public void ChangeState(
            bool? isPlaying,
            string? currentSong,
            byte? volume)
        {
            IsPlaying = isPlaying ?? IsPlaying;
            CurrentSong = currentSong ?? CurrentSong;
            Volume = volume ?? Volume;
        }
    }
}
