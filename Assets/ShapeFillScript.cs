using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KModkit;
using System.Text.RegularExpressions;
using System.Text;

public class ShapeFillScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;

    public KMSelectable[] SquareSels;
    public Texture[] ShapeTextures;
    public GameObject[] SquareObjs;
    public GameObject[] SolveTextObj;
    public TextMesh ScreenText;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private static readonly string[] _shapeNames = { "Circle", "Diamond", "Heart", "Octagon", "Square", "Star", "Trapezoid", "Triangle" };
    private static readonly string[] _fillNames = { "Cross", "Diagonal", "Dots", "Empty", "Fill", "Horizontal", "Vertical", "X" };
    private int[][][] _gridIxs = new int[2][][]
    {
        new int[5][] { new int[5], new int[5], new int[5], new int[5], new int[5] },
        new int[5][] { new int[5], new int[5], new int[5], new int[5], new int[5] }
    };

    private int _stage = 0;
    private int[] _currentShapeFillConfig = new int[2];
    private int[] _correctIxs = new int[2];
    private int[] _correctShapeFillPress = new int[2];
    private int[][] _displayIxs = new int[5][] { new int[5], new int[5], new int[5], new int[5], new int[5] };
    private bool _isAnimating = true;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        for (int btn = 0; btn < SquareSels.Length; btn++)
            SquareSels[btn].OnInteract += SquarePress(btn);
        for (int i = 0; i < 25; i++)
            SquareObjs[i].SetActive(false);
        Module.OnActivate += Activate;
        Debug.LogFormat("[Shape Fill #{0}] Shape abbreviations: [C]IRCLE, [D]IAMOND, [H]EART, [O]CTAGON, [S]QUARE, STA[R], TRAPE[Z]OID, [T]RIANGLE.", _moduleId);
        Debug.LogFormat("[Shape Fill #{0}] Fill abbreviations: [C]ROSS, DIA[G]ONAL, [D]OTS, [E]MPTY, [F]ILL, [H]ORIZONTAL, [V]ERTICAL, [X].", _moduleId);
    }

    private void Activate()
    {
        StartCoroutine(FadeAnimation(false, true));
    }

    private KMSelectable.OnInteractHandler SquarePress(int btn)
    {
        return delegate ()
        {
            SquareSels[btn].AddInteractionPunch(0.5f);
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
            if (_isAnimating || _moduleSolved)
                return false;
            if (btn == _correctIxs[0] * 5 + _correctIxs[1])
            {
                Debug.LogFormat("[Shape Fill #{0}] Pressed correct cell at {1}. Moving to stage {2}.", _moduleId, ConvToCoord(btn), _stage + 2);
                StartCoroutine(FadeAnimation(false));
            }
            else
            {
                Module.HandleStrike();
                Debug.LogFormat("[Shape Fill #{0}] Incorrectly pressed cell at {1}. Strike. Resetting...", _moduleId, ConvToCoord(btn));
                StartCoroutine(FadeAnimation(true));
            }
            return false;
        };
    }

    private string ConvToCoord(int cell)
    {
        return "ABCDE"[cell % 5].ToString() + "12345"[cell / 5].ToString();
    }

    private void GenerateStage()
    {
        int attempts = 0;
        generate:
        attempts++;
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                var rnd = Rnd.Range(0, ShapeTextures.Length);
                _gridIxs[0][i][j] = rnd / 8;
                _gridIxs[1][j][i] = rnd % 8;
                _displayIxs[i][j] = rnd;
            }
        }
        var usedShapes = new List<int>();
        var usedFills = new List<int>();
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                if (!usedShapes.Contains(_gridIxs[0][i][j]))
                    usedShapes.Add(_gridIxs[0][i][j]);
                if (!usedFills.Contains(_gridIxs[1][i][j]))
                    usedFills.Add(_gridIxs[1][i][j]);
            }
        }
        if (((usedShapes.Count != 7 || usedFills.Count != 7) && _stage == 0) || ((usedShapes.Count != 8 || usedFills.Count != 8) && _stage != 0))
            goto generate;
        if (_stage == 0)
        {
            for (int i = 0; i < 8; i++)
            {
                if (!usedShapes.Contains(i))
                    _currentShapeFillConfig[0] = i;
                if (!usedFills.Contains(i))
                    _currentShapeFillConfig[1] = i;
            }
        }
        else
        {
            _currentShapeFillConfig[0] = _correctShapeFillPress[0];
            _currentShapeFillConfig[1] = _correctShapeFillPress[1];
        }
        for (int sfc = 0; sfc < 2; sfc++)
        {
            var checkList = new List<int>();
            for (int ixFirst = 0; ixFirst < 5; ixFirst++)
            {
                if (_currentShapeFillConfig[sfc] == 0)
                {
                    if (_stage == 0)
                    {
                        if (!_gridIxs[sfc][ixFirst].Contains(4))
                            checkList.Add(ixFirst);
                    }
                    else if (_stage == 1)
                    {
                        var c = new List<int>();
                        for (int ixSecond = 0; ixSecond < 5; ixSecond++)
                            if (!c.Contains(_gridIxs[sfc][ixFirst][ixSecond]))
                                c.Add(_gridIxs[sfc][ixFirst][ixSecond]);
                        if (c.Count == 3)
                            checkList.Add(ixFirst);
                    }
                    else
                    {
                        var c = new List<int>();
                        for (int ixSecond = 0; ixSecond < 5; ixSecond++)
                        {
                            if (!c.Contains(_gridIxs[sfc][(ixFirst + 1) % 5][ixSecond]))
                                c.Add(_gridIxs[sfc][(ixFirst + 1) % 5][ixSecond]);
                            if (!c.Contains(_gridIxs[sfc][(ixFirst + 4) % 5][ixSecond]))
                                c.Add(_gridIxs[sfc][(ixFirst + 4) % 5][ixSecond]);
                        }
                        if (c.Count == 4 || c.Count == 6)
                            checkList.Add(ixFirst);
                    }
                }
                else if (_currentShapeFillConfig[sfc] == 1)
                {
                    if (_stage == 0)
                    {
                        int c = 0;
                        for (int ixSecond = 0; ixSecond < 5; ixSecond++)
                            if (_gridIxs[sfc][ixFirst][ixSecond] == 2)
                                c++;
                        if (c == 1)
                            checkList.Add(ixFirst);
                    }
                    else if (_stage == 1)
                    {
                        if ((_gridIxs[sfc][ixFirst].Contains(1) && !_gridIxs[sfc][ixFirst].Contains(3)) || (!_gridIxs[sfc][ixFirst].Contains(1) && _gridIxs[sfc][ixFirst].Contains(3)))
                            checkList.Add(ixFirst);
                    }
                    else
                    {
                        if (_gridIxs[sfc][ixFirst].Distinct().Count() == _gridIxs[sfc][(ixFirst + 4) % 5].Distinct().Count())
                            checkList.Add(ixFirst);
                    }
                }
                else if (_currentShapeFillConfig[sfc] == 2)
                {
                    if (_stage == 0)
                    {
                        int c = 0;
                        for (int ixSecond = 0; ixSecond < 5; ixSecond++)
                            if (_gridIxs[sfc][ixFirst][ixSecond] == 6)
                                c++;
                        if (c == 2)
                            checkList.Add(ixFirst);
                    }
                    else if (_stage == 1)
                    {
                        int c1 = 0;
                        int c2 = 0;
                        for (int ixSecond = 0; ixSecond < 5; ixSecond++)
                        {
                            if (_gridIxs[sfc][ixFirst][ixSecond] == 1)
                                c1++;
                            if (_gridIxs[sfc][ixFirst][ixSecond] == 4)
                                c2++;
                        }
                        if (c1 == c2)
                            checkList.Add(ixFirst);
                    }
                    else
                    {
                        var c = new List<int>();
                        for (int ixSecond = 0; ixSecond < 5; ixSecond++)
                        {
                            if (!c.Contains(_gridIxs[sfc][ixFirst][ixSecond]))
                                c.Add(_gridIxs[sfc][ixFirst][ixSecond]);
                            if (!c.Contains(_gridIxs[sfc][(ixFirst + 2) % 5][ixSecond]))
                                c.Add(_gridIxs[sfc][(ixFirst + 2) % 5][ixSecond]);
                        }
                        if (c.Count == 7)
                            checkList.Add(ixFirst);
                    }
                }
                else if (_currentShapeFillConfig[sfc] == 3)
                {
                    if (_stage == 0)
                    {
                        if (!_gridIxs[sfc][ixFirst].Contains(5) && !_gridIxs[sfc][ixFirst].Contains(6))
                            checkList.Add(ixFirst);
                    }
                    else if (_stage == 1)
                    {
                        int c = 0;
                        if (_gridIxs[sfc][ixFirst].Contains(2))
                            c++;
                        if (_gridIxs[sfc][ixFirst].Contains(7))
                            c++;
                        if (c != 1)
                            checkList.Add(ixFirst);
                    }
                    else
                    {
                        int c = 0;
                        if (_gridIxs[sfc][ixFirst].Contains(0))
                            c++;
                        if (_gridIxs[sfc][ixFirst].Contains(4))
                            c++;
                        if (_gridIxs[sfc][ixFirst].Contains(1))
                            c++;
                        if (c == 2)
                            checkList.Add(ixFirst);
                    }
                }
                else if (_currentShapeFillConfig[sfc] == 4)
                {
                    if (_stage == 0)
                    {
                        if (_gridIxs[sfc][ixFirst].Contains(1) && _gridIxs[sfc][ixFirst].Contains(7))
                            checkList.Add(ixFirst);
                    }
                    else if (_stage == 1)
                    {
                        var c = new List<int>();
                        for (int ixSecond = 0; ixSecond < 5; ixSecond++)
                        {
                            if (!c.Contains(_gridIxs[sfc][ixFirst][ixSecond]))
                                c.Add(_gridIxs[sfc][ixFirst][ixSecond]);
                        }
                        if (c.Count % 2 == 0)
                            checkList.Add(ixFirst);
                    }
                    else
                    {
                        var c1 = new List<int>();
                        var c2 = new List<int>();
                        for (int ixSecond = 0; ixSecond < 5; ixSecond++)
                        {
                            if (!c1.Contains(_gridIxs[sfc][ixFirst][ixSecond]))
                                c1.Add(_gridIxs[sfc][ixFirst][ixSecond]);
                            if (!c2.Contains(_gridIxs[sfc][(ixFirst + 1) % 5][ixSecond]))
                                c2.Add(_gridIxs[sfc][(ixFirst + 1) % 5][ixSecond]);
                        }
                        if (c1.Count == c2.Count + 2)
                            checkList.Add(ixFirst);
                    }
                }
                else if (_currentShapeFillConfig[sfc] == 5)
                {
                    if (_stage == 0)
                    {
                        int c = 0;
                        for (int ixSecond = 0; ixSecond < 5; ixSecond++)
                            if (_gridIxs[sfc][ixFirst][ixSecond] == 0)
                                c++;
                        if (c == 1)
                            checkList.Add(ixFirst);
                    }
                    else if (_stage == 1)
                    {
                        if (_gridIxs[sfc][ixFirst].Distinct().Count() == 5)
                            checkList.Add(ixFirst);
                    }
                    else
                    {
                        int c = 0;
                        for (int ixSecond = 0; ixSecond < 5; ixSecond++)
                            if (_gridIxs[sfc][ixFirst][ixSecond] == 1 || _gridIxs[sfc][ixFirst][ixSecond] == 4 || _gridIxs[sfc][ixFirst][ixSecond] == 6)
                                c++;
                        if (c == 3 || c == 5)
                            checkList.Add(ixFirst);
                    }
                }
                else if (_currentShapeFillConfig[sfc] == 6)
                {
                    if (_stage == 0)
                    {
                        if (!_gridIxs[sfc][ixFirst].Contains(3))
                            checkList.Add(ixFirst);
                    }
                    else if (_stage == 1)
                    {
                        if ((_gridIxs[sfc][ixFirst].Contains(0) && _gridIxs[sfc][ixFirst].Contains(3)) || (!_gridIxs[sfc][ixFirst].Contains(0) && !_gridIxs[sfc][ixFirst].Contains(3)))
                            checkList.Add(ixFirst);
                    }
                    else
                    {
                        var c = new List<int>();
                        for (int ixSecond = 0; ixSecond < 5; ixSecond++)
                        {
                            if (!c.Contains(_gridIxs[sfc][ixFirst][ixSecond]))
                                c.Add(_gridIxs[sfc][ixFirst][ixSecond]);
                            if (!c.Contains(_gridIxs[sfc][(ixFirst + 3) % 5][ixSecond]))
                                c.Add(_gridIxs[sfc][(ixFirst + 3) % 5][ixSecond]);
                        }
                        if (c.Count == 6)
                            checkList.Add(ixFirst);
                    }
                }
                else
                {
                    if (_stage == 0)
                    {
                        int c = 0;
                        for (int ixSecond = 0; ixSecond < 5; ixSecond++)
                            if (_gridIxs[sfc][ixFirst][ixSecond] == 4)
                                c++;
                        if (c == 2)
                            checkList.Add(ixFirst);
                    }
                    else if (_stage == 1)
                    {
                        if ((_gridIxs[sfc][ixFirst].Contains(1) && _gridIxs[sfc][ixFirst].Contains(7)) || (!_gridIxs[sfc][ixFirst].Contains(1) && !_gridIxs[sfc][ixFirst].Contains(7)))
                            checkList.Add(ixFirst);
                    }
                    else
                    {
                        var c1 = new List<int>();
                        var c2 = new List<int>();
                        for (int ixSecond = 0; ixSecond < 5; ixSecond++)
                        {
                            if (!c1.Contains(_gridIxs[sfc][(ixFirst + 2) % 5][ixSecond]))
                                c1.Add(_gridIxs[sfc][(ixFirst + 2) % 5][ixSecond]);
                            if (!c2.Contains(_gridIxs[sfc][(ixFirst + 3) % 5][ixSecond]))
                                c2.Add(_gridIxs[sfc][(ixFirst + 3) % 5][ixSecond]);
                        }
                        if (c1.Count == c2.Count)
                            checkList.Add(ixFirst);
                    }
                }
            }
            if (checkList.Count != 1)
                goto generate;
            _correctIxs[sfc] = checkList[0];
        }
        for (int i = 0; i < 5; i++)
            for (int j = 0; j < 5; j++)
                SquareObjs[i * 5 + j].GetComponent<MeshRenderer>().material.mainTexture = ShapeTextures[_displayIxs[i][j]];
        _correctShapeFillPress[0] = _displayIxs[_correctIxs[0]][_correctIxs[1]] / 8;
        _correctShapeFillPress[1] = _displayIxs[_correctIxs[0]][_correctIxs[1]] % 8;
        Debug.LogFormat("[Shape Fill #{0}] Stage {3} shape and fill: {1} {2}", _moduleId, _shapeNames[_currentShapeFillConfig[0]], _fillNames[_currentShapeFillConfig[1]], _stage + 1);
        var s = "CDHOSRZT";
        var f = "CGDEFHVX";
        Debug.LogFormat("[Shape Fill #{0}] Grid:", _moduleId);
        for (int i = 0; i < 5; i++)
        {
            var str = "";
            for (int j = 0; j < 5; j++)
            {
                str += (s[_displayIxs[i][j] / 8]);
                str += (f[_displayIxs[i][j] % 8]);
                if (j != 4)
                    str += (" ");
            }
            Debug.LogFormat("[Shape Fill #{0}] {1}", _moduleId, str);
        }
        Debug.LogFormat("[Shape Fill #{0}] Correct cell to press: {1}", _moduleId, ConvToCoord(_correctIxs[0] * 5 + _correctIxs[1]));
        Debug.LogFormat("<Shape Fill #{0}> Attempts to generate a grid at stage {1}: {2}", _moduleId, _stage + 1, attempts);
    }

    private IEnumerator FadeAnimation(bool isStrike, bool isActivate = false)
    {
        _isAnimating = true;
        if (!isActivate)
        {
            for (int i = 0; i < 25; i++)
            {
                SquareObjs[i].SetActive(false);
                yield return new WaitForSeconds(0.03f);
            }
            if (!isStrike)
                _stage++;
            else
                _stage = 0;
        }
        if (_stage == 3)
        {
            for (int i = 0; i < 25; i++)
                SquareObjs[i].GetComponent<MeshRenderer>().material.mainTexture = ShapeTextures[Rnd.Range(0, ShapeTextures.Length)];
            StartCoroutine(SolveAnimation());
            yield break;
        }
        GenerateStage();
        for (int i = 0; i < 25; i++)
        {
            SquareObjs[i].SetActive(true);
            yield return new WaitForSeconds(0.03f);
        }
        var stageTxts = new string[] { "stage 1", "stage 2", "stage 3" };
        for (int i = 0; i <= stageTxts[_stage].Length; i++)
        {
            ScreenText.text = stageTxts[_stage].Substring(0, i);
            yield return new WaitForSeconds(0.05f);
        }
        _isAnimating = false;
    }

    private IEnumerator SolveAnimation()
    {
        Audio.PlaySoundAtTransform("Nokia", transform);
        _moduleSolved = true;
        var youDidIt = new int[] { 6, 7, 8, 11, 12, 13, 16, 17, 18 };
        var textIxs = new int[] { 0, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0, 3, 4, 5, 0, 0, 6, 7, 8, 0, 0, 0, 0, 0, 0 };
        for (int i = 0; i < 25; i++)
        {
            if (!youDidIt.Contains(i))
                SquareObjs[i].SetActive(true);
            else
                SolveTextObj[textIxs[i]].SetActive(true);
            yield return new WaitForSeconds(0.03f);
        }
        for (int i = 0; i <= "solved".Length; i++)
        {
            ScreenText.text = "solved".Substring(0, i);
            yield return new WaitForSeconds(0.05f);
        }
        Module.HandlePass();
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} press C3 [Press cell at column C, row 3. Columns are labelled A-E from left to right. Rows are labelled 1-5 from top to bottom. 'press' is optional.]";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        var m = Regex.Match(command, @"^\s*(press\s+)?([A-E])([1-5])\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!m.Success)
            yield break;
        yield return null;
        yield return "strike";
        yield return "solve";
        while (_isAnimating)
            yield return null;
        SquareSels[m.Groups[2].Value.ToUpperInvariant()[0] - 'A' + (m.Groups[3].Value[0] - '0' - 1) * 5].OnInteract();
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        for (int i = _stage; i < 3; i++)
        {
            while (_isAnimating)
                yield return true;
            SquareSels[_correctIxs[0] * 5 + _correctIxs[1]].OnInteract();
        }
        while (!_moduleSolved)
            yield return true;
    }
}