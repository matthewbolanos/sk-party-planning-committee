using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using OpenTK.Audio.OpenAL;
using System.IO;
using System.Threading.Tasks;

namespace MusicAPI.Controllers
{
    [ApiController]
    [Route("Speaker")]
    public class SpeakerController : ControllerBase
    {
        private static int _buffer;
        private static int _source;
        private static ALDevice _device;
        private static ALContext _context;

        /// <summary>
        /// Loads a song.
        /// Call this before playing to ensure the song can immediately start playing when Play is called.
        /// </summary>
        /// <param name="songRequest">An object containing the relative path to the song file.</param>
        /// <returns>A message indicating whether the song was loaded successfully.</returns>
        /// <response code="200">Song loaded successfully.</response>
        /// <response code="400">Error loading song.</response>
        [HttpPost("LoadSong", Name = "load_song")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Description("Loads a song; call this before playing to ensure the song can immediately start playing when Play is called.")]
        public IActionResult LoadSong([FromBody] SongRequest songRequest)
        {
            try
            {
                // Check if relativePath starts with "/mnt/data/"; if so, remove it
                var relativePath = songRequest.RelativePath;
                if (relativePath.StartsWith("/mnt/data/"))
                {
                    relativePath = relativePath.Substring(10);
                }

                // Read WAV file and determine the format and sample rate
                (ALFormat format, int sampleRate, byte[] soundData) = LoadWavFile(relativePath);

                // Initialize the OpenAL audio context
                _device = ALC.OpenDevice(null); // null for the default device
                _context = ALC.CreateContext(_device, (int[])null);
                ALC.MakeContextCurrent(_context);

                // Generate a buffer and source
                _buffer = AL.GenBuffer();
                _source = AL.GenSource();

                // Pin the soundData array in memory
                GCHandle handle = GCHandle.Alloc(soundData, GCHandleType.Pinned);
                try
                {
                    IntPtr pointer = handle.AddrOfPinnedObject();
                    AL.BufferData(_buffer, format, pointer, soundData.Length, sampleRate);
                }
                finally
                {
                    if (handle.IsAllocated)
                    {
                        handle.Free(); // Make sure to free the handle to avoid memory leaks
                    }
                }

                // Bind the buffer with the source
                AL.Source(_source, ALSourcei.Buffer, _buffer);

                return Ok("Song loaded successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error loading song: {ex.Message}");
            }
        }

        public class SongRequest
        {
            public string RelativePath { get; set; }
        }


        /// <summary>
        /// Plays a song.
        /// You must call LoadSong before calling this.
        /// </summary>
        /// <returns>A message indicating whether the song is playing.</returns>
        /// <response code="200">Song is playing.</response>
        /// <response code="400">Error playing song.</response>
        [HttpPost("Play", Name = "play_song")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Description("Plays a song; you must call LoadSong before calling this.")]
        public IActionResult Play()
        {
            try
            {
                // Play asynchronously in a new task
                Task.Run(() =>
                {
                    // play it
                    AL.SourcePlay(_source);

                    // Simple playback loop
                    AL.GetSource(_source, ALGetSourcei.SourceState, out int state);

                    // Loop until the sound has finished playing
                    while (state == (int)ALSourceState.Playing)
                    {
                        System.Threading.Thread.Sleep(100); // Sleep to prevent a tight loop, adjust as needed
                        AL.GetSource(_source, ALGetSourcei.SourceState, out state);
                    }

                    // Cleanup
                    AL.DeleteSource(_source);
                    AL.DeleteBuffer(_buffer);
                    ALC.DestroyContext(_context);
                    ALC.CloseDevice(_device);

                });

                return Ok("Song is playing.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error playing song: {ex.Message}");
            }
        }

        private static (ALFormat format, int sampleRate, byte[] data) LoadWavFile(string filePath)
        {
            using (BinaryReader reader = new BinaryReader(System.IO.File.OpenRead(filePath)))
            {
                // Chunk ID
                string chunkId = new string(reader.ReadChars(4));
                if (chunkId != "RIFF")
                {
                    throw new FormatException("Invalid WAV file format: Missing 'RIFF' identifier.");
                }

                // Chunk Size
                int chunkSize = reader.ReadInt32();

                // Format
                string format = new string(reader.ReadChars(4));
                if (format != "WAVE")
                {
                    throw new FormatException("Invalid WAV file format: Missing 'WAVE' identifier.");
                }

                // Sub-chunk 1 ID
                string subChunk1Id = new string(reader.ReadChars(4));
                if (subChunk1Id != "fmt ")
                {
                    throw new FormatException("Invalid WAV file format: Missing 'fmt ' identifier.");
                }

                // Sub-chunk 1 size
                int subChunk1Size = reader.ReadInt32();

                // Audio format (1 = PCM)
                int audioFormat = reader.ReadInt16();
                if (audioFormat != 1)
                {
                    throw new NotSupportedException("Only PCM audio format is supported.");
                }

                // Number of channels
                int numChannels = reader.ReadInt16();

                // Sample rate
                int sampleRate = reader.ReadInt32();

                // Byte rate
                int byteRate = reader.ReadInt32();

                // Block align
                int blockAlign = reader.ReadInt16();

                // Bits per sample
                int bitsPerSample = reader.ReadInt16();

                // Determine the ALFormat
                ALFormat alFormat;
                if (numChannels == 1 && bitsPerSample == 8)
                {
                    alFormat = ALFormat.Mono8;
                }
                else if (numChannels == 1 && bitsPerSample == 16)
                {
                    alFormat = ALFormat.Mono16;
                }
                else if (numChannels == 2 && bitsPerSample == 8)
                {
                    alFormat = ALFormat.Stereo8;
                }
                else if (numChannels == 2 && bitsPerSample == 16)
                {
                    alFormat = ALFormat.Stereo16;
                }
                else
                {
                    throw new NotSupportedException("Unsupported channel or bit depth format.");
                }

                // Skip optional chunks to reach the data chunk
                string subChunk2Id;
                int subChunk2Size;
                do
                {
                    subChunk2Id = new string(reader.ReadChars(4));
                    subChunk2Size = reader.ReadInt32();

                    if (subChunk2Id == "LIST")
                    {
                        // Skip the LIST chunk
                        reader.BaseStream.Seek(subChunk2Size, SeekOrigin.Current);
                    }
                } while (subChunk2Id != "data");

                // Audio data
                byte[] data = reader.ReadBytes(subChunk2Size);

                return (alFormat, sampleRate, data);
            }
        }
    }
}
