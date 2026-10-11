namespace SlotMachine.Core
{
    /// <summary>
    /// Identifiers for the slot machine symbols.
    /// </summary>
    public enum SymbolId
    {
        Cherry = 0,
        Bar = 1,
        Bell = 2,
        Seven = 3,
        Wild = 4
    }

    /// <summary>
    /// Current state of the slot machine state machine.
    /// </summary>
    public enum SlotState
    {
        Idle,
        LeverPulling,
        Spinning,
        Stopping,
        Evaluating,
        FreeSpinsBonus
    }

    /// <summary>
    /// Payline definitions across 3 reels and 3 rows.
    /// </summary>
    public enum PaylineId
    {
        Middle = 0,
        Top = 1,
        Bottom = 2,
        DiagonalDown = 3,
        DiagonalUp = 4
    }
}
