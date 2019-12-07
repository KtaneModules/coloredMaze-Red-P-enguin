using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class coloredMazeScript : MonoBehaviour
{
    //i kinda need this stuff
    public KMBombInfo bomb;
    public KMAudio audio;
    public KMBombModule Module;
    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool moduleSolved = false;

    //buttons are put in array in reading order
    public KMSelectable[] buttons;

    //13x13
    private string maze =
        "+-----------+" +
        "|R.O|Y.G.B.V|" +
        "|.|.|.|.|.|.|" +
        "|G|O.R|V|B|Y|" +
        "|.|.--+-+.|.|" +
        "|V|B.G|O.R|Y|" +
        "|.+-+-+.+-+.|" +
        "|O.R|V.Y|G|B|" +
        "|---+.|.|.|.|" +
        "|B.Y.V|R|G.O|" +
        "|.----+-+--.|" +
        "|V.G.B.R|Y.O|" +
        "+-----------+";
    private int[] corners = { 14, 24, 144, 154 };

    //finding starting coordinate
    int[,] startingPos = new int[4, 4]
    { { 42, 44, 46, 48 },
    { 68, 70, 72, 74 },
    { 94, 96, 98, 100 },
    { 120, 122, 124, 126 } };
    private int startingrow = 0;
    private int startingcolumn = 0;
    private bool rowFound = false;
    private bool columnFound = false;
    private char[] alphabet = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' };

    //moving stuff
    private int position = 0;
    //this checks in this order: left, up, right, down
    private int[] movingChecks = { -2, -26, 2, 26};
    private int[] checkForWalls = { -1, -13, 1, 13 };
    private string[] directions = { "Left", "Up", "Right", "Down" }; //used for logging

    // Use this for initialization
    void Start()
    {
        for (int i = 0; i < 6; i++)
        {
            if(rowFound && columnFound)
            {
                i = 6; //if coordinates found stops it from checking every other position in serial #
            }
            else
            {
                if(char.IsLetter(bomb.GetSerialNumber()[i]) && !columnFound) //is this position a letter and there wasn't a letter before
                {
                    columnFound = true;
                    for(int j = 0; j < 26; j++)
                    {
                        if(alphabet[j] == char.ToLower(bomb.GetSerialNumber()[i]))
                        {
                            startingcolumn = (j + 1) % 4;
                            j = 26;
                        }
                    }
                }
                else if(char.IsDigit(bomb.GetSerialNumber()[i]) && !rowFound) //otherwise this is a number, the rowFound check is just to make sure this works
                {
                    rowFound = true;
                    startingrow = bomb.GetSerialNumber()[i] % 4;
                }
            }
        }
        DebugMsg("The number that determines the column is " + (startingcolumn + 1) + ".");
        DebugMsg("The number that determines the row is " + (startingrow + 1) + ".");
        moduleReset(); //set position
    }

    void moduleReset()
    {
        position = startingPos[startingrow, startingcolumn];
        DebugMsg("" + position);
    }

    void Awake()
    {
        ModuleId = ModuleIdCounter++;

        foreach (KMSelectable button in buttons)
        {
            KMSelectable pressedButton = button;
            button.OnInteract += delegate () { buttonPressed(pressedButton); return false; };
        }
    }

    void buttonPressed(KMSelectable pressedButton)
    {
        pressedButton.AddInteractionPunch();
        if (moduleSolved == true)
        {
            return;
        }
        if (pressedButton == buttons[0])
        {
            DebugMsg("Reset button pushed. Resetting module.");
            moduleReset();
        }
        else
        {
            for (int i = 0; i < 4; i++)
            {
                if (position + movingChecks[i] > 0 && position + movingChecks[i] < 168)
                {
                    if (maze[position + movingChecks[i]].ToString() == pressedButton.name) //figures out which direction to go, if any
                    {
                        if (maze[position + checkForWalls[i]] != '.')
                        {
                            DebugMsg("Ran into a wall. Module striked.");
                            Module.HandleStrike();
                            audio.PlaySoundAtTransform("strike", transform);
                        }
                        else
                        {
                            position = position + movingChecks[i];
                            DebugMsg("Moving " + directions[i] + ".");
                            if (corners.Contains(position)) //if you're in a corner
                            {
                                DebugMsg("In a corner. Module solved!");
                                moduleSolved = true;
                                audio.PlaySoundAtTransform("solve", transform);
                                Module.HandlePass();
                            }
                            else
                            {
                                audio.PlaySoundAtTransform(pressedButton.name, transform);
                            }
                        }
                        i = 4;
                    }
                    else if (i == 3) //if there isn't any adjacent squares with color pressed
                    {
                        DebugMsg("No adjecent squares with that color. Module striked.");
                        Module.HandleStrike();
                        audio.PlaySoundAtTransform("strike", transform);
                    }
                }
            }
        }
    }

    private bool isCommandValid(string cmd)
    {
        string[] validbtns = { "r", "o", "y", "g", "b", "c", "v", "p", "l", "red", "orange", "yellow", "green", "cyan", "blue", "violet", "purple", "lime" }; //does the command only contain these if it's not !{0} reset

        string[] btnSequence = cmd.ToLowerInvariant().Split(new[] { ' ' });

        foreach (var btn in btnSequence)
        {
            if (!validbtns.Contains(btn.ToLower()))
            {
                return false;
            }
        }
        return true;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} red presses the red button. !{0} reset resets the module.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string cmd)
    {
        var parts = cmd.ToLowerInvariant().Split(new[] { ' ' });

        if (parts.Length == 1 && parts[0].ToLower() == "reset")
        {
            yield return null;
            yield return new KMSelectable[] { buttons[0] };
        }
        if (isCommandValid(cmd))
        {
            yield return null;
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].ToLower() == "r" || parts[i].ToLower() == "red")
                {
                    yield return new KMSelectable[] { buttons[1] };
                }
                if (parts[i].ToLower() == "o" || parts[i].ToLower() == "orange")
                {
                    yield return new KMSelectable[] { buttons[2] };
                }
                if (parts[i].ToLower() == "y" || parts[i].ToLower() == "yellow")
                {
                    yield return new KMSelectable[] { buttons[3] };
                }
                if (parts[i].ToLower() == "g" || parts[i].ToLower() == "green" || parts[i].ToLower() == "l" || parts[i].ToLower() == "lime")
                {
                    yield return new KMSelectable[] { buttons[4] };
                }
                if (parts[i].ToLower() == "b" || parts[i].ToLower() == "c" || parts[i].ToLower() == "blue" || parts[i].ToLower() == "cyan")
                {
                    yield return new KMSelectable[] { buttons[5] };
                }
                if (parts[i].ToLower() == "p" || parts[i].ToLower() == "v" || parts[i].ToLower() == "purple" || parts[i].ToLower() == "violet")
                {
                    yield return new KMSelectable[] { buttons[6] };
                }
            }
        }
        else
        {
            yield break;
        }
    }

    void DebugMsg(string msg)
    {
        Debug.LogFormat("[The Colored Maze #{0}] {1}", ModuleId, msg);
    }
}
