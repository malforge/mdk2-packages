namespace Lumel.Input
{
    /// <summary>What a key means when no element handled the key itself, whichever key it was.</summary>
    public enum Intent
    {
        /// <summary>Accept the answer on offer; what Enter means.</summary>
        Accept,

        /// <summary>Press what holds the keyboard, and nothing else; what Space means.</summary>
        Press,

        /// <summary>Dismiss what is open without an answer; what Escape means.</summary>
        Cancel,

        /// <summary>Move focus to the nearest element above; what the Up arrow means.</summary>
        Up,

        /// <summary>Move focus to the nearest element below; what the Down arrow means.</summary>
        Down,

        /// <summary>Move focus to the nearest element to the left; what the Left arrow means.</summary>
        Left,

        /// <summary>Move focus to the nearest element to the right; what the Right arrow means.</summary>
        Right
    }
}
