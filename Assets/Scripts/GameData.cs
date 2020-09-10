using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameData
{
    public long numPlayers;

    public GameData(long numP)
    {
        numPlayers = numP;
    }

}
