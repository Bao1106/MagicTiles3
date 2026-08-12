using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    Menu,
    Playing,
    Paused,
    GameOver
}

public enum Judgement
{
    Perfect,
    Good,
    Miss
}

public enum NoteKind
{
    Normal,

    /// <summary>Hitting it permanently raises the fall speed for every note after it.</summary>
    SpeedUp
}