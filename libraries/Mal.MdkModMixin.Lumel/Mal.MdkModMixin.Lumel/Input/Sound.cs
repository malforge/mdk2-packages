namespace Lumel.Input
{
    /// <summary>A sound a control plays, named by the game's sound cue.</summary>
    public sealed class Sound
    {
        /// <summary>No sound, which silences a sound set by a look beneath.</summary>
        public static readonly Sound None = new Sound();

        /// <summary>Creates the sound the game plays for the cue <paramref name="name" />.</summary>
        public Sound(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new System.ArgumentException("A sound needs the name of a cue.", nameof(name));

            Name = name;
        }

        Sound()
        {
        }

        /// <summary>The name of the game's sound cue, or null for <see cref="None" />.</summary>
        public string Name { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return Name ?? "None";
        }
    }
}
