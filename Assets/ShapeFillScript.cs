using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KModkit;
using System.Text.RegularExpressions;

public class ShapeFillScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;

    public KMSelectable[] SquareSels;
    public KMSelectable SubmitSel;
    public KMSelectable ToggleSel;
    public KMSelectable ResetSel;
    public GameObject Light;
    public Material[] LightMats;
    public Texture[] ShapeTextures;

    public TextMesh ToggleText;
    public GameObject[] ShapeFillObjs;
    public GameObject SubmitShape;
    public GameObject ToggleFill;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private int[] InitialShapeInfo = new int[25];
    private int[] CurrentShapeInfo = new int[25];
    private bool _toggleIsFill;

    private readonly string[] SHAPENAMES = { "Circle", "Diamond", "Heart", "Octagon", "Square", "Star", "Trapezoid", "Triangle" };
    private readonly string[] FILLNAMES = { "Cross", "Diagonal", "Dots", "Empty", "Fill", "Horizontal", "Vertical", "X" };

    private readonly List<int> _restrictions = new List<int>();
    private int[] _shapeRestrictions;
    private int[] _fillRestrictions;
    private int[][] restrIx = new int[16][]
    {
        new int[4] {9, 0, 2, 3},
        new int[4] {12, 7, 4, 6},
        new int[4] {8, 5, 10, 1},
        new int[4] {11, 4, 12, 5},
        new int[4] {9, 2, 6, 0},
        new int[4] {11, 8, 1, 3},
        new int[4] {7, 10, 12, 3},
        new int[4] {1, 9, 5, 6},

        new int[4] {23, 24, 15, 13},
        new int[4] {16, 14, 25, 18},
        new int[4] {19, 17, 21, 22},
        new int[4] {20, 19, 16, 25},
        new int[4] {24, 20, 18, 21},
        new int[4] {22, 14, 13, 23},
        new int[4] {17, 15, 16, 22},
        new int[4] {25, 18, 13, 24}
    };

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        Light.GetComponent<MeshRenderer>().material = LightMats[0];

        for (int i = 0; i < SquareSels.Length; i++)
            SquareSels[i].OnInteract += SquarePress(i);
        SubmitSel.OnInteract += SubmitPress;
        ToggleSel.OnInteract += TogglePress;
        ResetSel.OnInteract += ResetPress;

        //Deciding the shapes and fills
        _shapeRestrictions = Enumerable.Range(0, 8).ToArray().Shuffle();
        _fillRestrictions = Enumerable.Range(0, 8).ToArray().Shuffle();

        for (int i = 0; i < ShapeFillObjs.Length; i++)
        {
        tryAgain2:
            int rnd = Rnd.Range(0, ShapeTextures.Length);
            if (rnd / 8 == _shapeRestrictions[0])
                goto tryAgain2;
            if (rnd % 8 == _fillRestrictions[0])
                goto tryAgain2;
            ShapeFillObjs[i].GetComponent<MeshRenderer>().material.mainTexture = ShapeTextures[rnd];
            InitialShapeInfo[i] = rnd;
            CurrentShapeInfo[i] = rnd;
        }
        Debug.LogFormat("[Shape Fill #{0}] The missing shape is {1}.", _moduleId, SHAPENAMES[_shapeRestrictions[0]]);
        Debug.LogFormat("[Shape Fill #{0}] The missing fill is {1}.", _moduleId, FILLNAMES[_fillRestrictions[0]]);

        ToggleFill.GetComponent<MeshRenderer>().material.mainTexture = ShapeTextures[_fillRestrictions[1]];
        SubmitShape.GetComponent<MeshRenderer>().material.mainTexture = ShapeTextures[_shapeRestrictions[1] * 8 + 3];
        Debug.LogFormat("[Shape Fill #{0}] The shape on the submit button is {1}.", _moduleId, SHAPENAMES[_shapeRestrictions[1]]);
        Debug.LogFormat("[Shape Fill #{0}] The fill on the toggle button is {1}.", _moduleId, FILLNAMES[_fillRestrictions[1]]);

        var str = "0123456789...ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var SerialNumber = BombInfo.GetSerialNumber().Take(4).Select(ch => str.IndexOf(ch) % 4).ToArray();

        _restrictions.Add(restrIx[_shapeRestrictions[1]][SerialNumber[0]]);
        var restr2 = restrIx[_shapeRestrictions[0]][SerialNumber[1]];
        while (_restrictions.Contains(restr2))
            restr2 = (restr2 + 3) % 13;
        _restrictions.Add(restr2);
        var restr3 = restrIx[_fillRestrictions[1] + 8][SerialNumber[2]];
        while (_restrictions.Contains(restr3))
            restr3 = ((restr3 + 3) % 13) + 13;
        _restrictions.Add(restr3);
        var restr4 = restrIx[_fillRestrictions[0] + 8][SerialNumber[3]];
        while (_restrictions.Contains(restr4))
            restr4 = ((restr4 + 3) % 13) + 13;
        _restrictions.Add(restr4);
        Debug.LogFormat("[Shape Fill #{0}] The restrictions in place are {1}", _moduleId, _restrictions.Select(r => (char)(r + 'A')).Join(", "));
        _restrictions.Add(26);
    }

    private bool TogglePress()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        if (!_moduleSolved)
        {
            Debug.LogFormat("[Shape Fill #{0}] Toggle press.", _moduleId);
            _toggleIsFill = !_toggleIsFill;
            ToggleText.text = _toggleIsFill ? "FILL" : "SHAPE";
        }
        return false;
    }

    private bool ResetPress()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        if (!_moduleSolved)
        {
            Debug.LogFormat("[Shape Fill #{0}] Reset press.", _moduleId);
            for (int i = 0; i < 25; i++)
            {
                CurrentShapeInfo[i] = InitialShapeInfo[i];
                ShapeFillObjs[i].GetComponent<MeshRenderer>().material.mainTexture = ShapeTextures[InitialShapeInfo[i]];
            }
            Light.GetComponent<MeshRenderer>().material = LightMats[0];
        }
        return false;
    }

    private KMSelectable.OnInteractHandler SquarePress(int shape)
    {
        return delegate ()
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
            if (!_moduleSolved)
            {
                Light.GetComponent<MeshRenderer>().material = LightMats[3];
                if (_toggleIsFill)
                    CurrentShapeInfo[shape] = ((CurrentShapeInfo[shape] / 8) * 8) + (((CurrentShapeInfo[shape] % 8) + 1) % 8);
                if (!_toggleIsFill)
                    CurrentShapeInfo[shape] = (CurrentShapeInfo[shape] + 8) % 64;
                ShapeFillObjs[shape].GetComponent<MeshRenderer>().material.mainTexture = ShapeTextures[CurrentShapeInfo[shape]];
            }
            return false;
        };
    }

    private bool SubmitPress()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        if (!_moduleSolved)
        {
            Debug.LogFormat("[Shape Fill #{0}] Submit press.", _moduleId);
            for (var restrIx = 0; restrIx < _restrictions.Count; restrIx++)
            {
                if (!_restDelegates[_restrictions[restrIx]](CurrentShapeInfo))
                {
                    Debug.LogFormat("[Shape Fill #{0}] Restriction #{1} ({2}) is violated.", _moduleId, restrIx + 1, _restrictions[restrIx] == 26 ? "default" : ((char)(_restrictions[restrIx] + 'A')).ToString());
                    Module.HandleStrike();
                    Light.GetComponent<MeshRenderer>().material = LightMats[2];
                    return false;
                }
            }
            Debug.LogFormat("[Shape Fill #{0}] Module solved.", _moduleId);
            Audio.PlaySoundAtTransform("Nokia", transform);
            Module.HandlePass();
            _moduleSolved = true;
            Light.GetComponent<MeshRenderer>().material = LightMats[1];
        }
        return false;
    }

    private static T[] newArray<T>(params T[] array) { return array; }

    private const int gridWidth = 5;
    private const int gridHeight = 5;

    /// <summary>Returns the set of cells adjacent to the specified cell (including diagonals).</summary>
    private static IEnumerable<int> Adjacent(int cell)
    {
        var x = cell % gridWidth;
        var y = cell / gridHeight;
        for (var xx = x - 1; xx <= x + 1; xx++)
            if (xx >= 0 && xx < gridWidth)
                for (var yy = y - 1; yy <= y + 1; yy++)
                    if (yy >= 0 && yy < gridHeight && (xx != x || yy != y))
                        yield return xx + gridWidth * yy;
    }

    /// <summary>Returns the set of cells orthogonally adjacent to the specified cell (no diagonals).</summary>
    private static IEnumerable<int> Orthogonal(int cell)
    {
        var x = cell % gridWidth;
        var y = cell / gridHeight;
        for (var xx = x - 1; xx <= x + 1; xx++)
            if (xx >= 0 && xx < gridWidth)
                for (var yy = y - 1; yy <= y + 1; yy++)
                    if (yy >= 0 && yy < gridHeight && (xx == x || yy == y) && (xx != x || yy != y))
                        yield return xx + gridWidth * yy;
    }

    /// <summary>Returns the set of cells orthogonally adjacent to the specified cell (no diagonals).</summary>
    private static IEnumerable<int> Diagonal(int cell)
    {
        return Adjacent(cell).Except(Orthogonal(cell));
    }

    private static readonly Func<int[], bool>[] _restDelegates = newArray<Func<int[], bool>>(
        /* A */ info => new[] { 6, 7, 8, 11, 12, 13, 16, 17, 18 }.All(cell => info[cell] / 8 != 6),
        /* B */ info => Enumerable.Range(0, 8).Count(shape => info.Count(s => s / 8 == shape) == 2) == 4,
        /* C */ info => Enumerable.Range(0, 25).All(cell => info[cell] / 8 != 7 || Orthogonal(cell).Any(c => info[c] / 8 == 7)),
        /* D */ info => new[] { 1, 3, 6, 8, 11, 13, 16, 18, 21, 23 }.Select(cell => info[cell] / 8).Distinct().Count() == 8,
        /* E */ info => !Enumerable.Range(0, 25).Any(cell => Diagonal(cell).Any(c => info[cell] / 8 == info[c] / 8)),
        /* F */ info => Enumerable.Range(0, 2).All(col => Enumerable.Range(0, 5).All(row => info[col + 5 * row] / 8 != 3)) || Enumerable.Range(3, 2).All(col => Enumerable.Range(0, 5).All(row => info[col + 5 * row] / 8 != 3)),
        /* G */ info => Enumerable.Range(0, 5).All(col => Enumerable.Range(0, 5).Count(row => info[col + 5 * row] / 8 == 4) == 1) && Enumerable.Range(0, 5).All(row => Enumerable.Range(0, 5).Count(col => info[col + 5 * row] / 8 == 4) == 1),
        /* H */ info => Enumerable.Range(0, 25).Count(cell => info[cell] / 8 == 1) == Enumerable.Range(0, 25).Where(cell => info[cell] / 8 == 1).Select(cell => info[cell] % 8).Distinct().Count(),
        /* I */ info => Enumerable.Range(0, 25).All(cell => cell % 5 == 2 || (cell % 5 < 2 ? info[cell] / 8 != 2 : info[cell] / 8 != 1)) || Enumerable.Range(0, 25).All(cell => cell % 5 == 2 || (cell % 5 < 2 ? info[cell] / 8 != 1 : info[cell] / 8 != 2)),
        /* J */ info => new[] { 0, 4, 12, 20, 24 }.Select(cell => info[cell] / 8).Distinct().Count() == 5,
        /* K */ info => new[] { 0, 1, 2, 3, 4, 5, 9, 10, 14, 15, 19, 20, 21, 22, 23, 24 }.All(cell => info[cell] / 8 != 0),
        /* L */ info => Enumerable.Range(0, 25).All(cell => info[cell] / 8 != 2 || info[cell] % 8 == 0),
        /* M */ info => Enumerable.Range(0, 5).Count(col => Enumerable.Range(0, 5).Any(row => info[col + 5 * row] / 8 == 5)) <= 1,
        /* N */ info => Enumerable.Range(0, 5).Count(row => Enumerable.Range(0, 5).Any(col => info[col + 5 * row] % 8 == 7)) <= 1,
        /* O */ info => !Enumerable.Range(0, 25).Any(cell => Diagonal(cell).Any(c => info[cell] % 8 == info[c] % 8)),
        /* P */ info => Enumerable.Range(0, 5).All(col => Enumerable.Range(0, 5).Count(row => info[col + 5 * row] % 8 == 3) == 1) && Enumerable.Range(0, 5).All(row => Enumerable.Range(0, 5).Count(col => info[col + 5 * row] % 8 == 3) == 1),
        /* Q */ info => new[] { 0, 4, 12, 20, 24 }.Select(cell => info[cell] % 8).Distinct().Count() == 5,
        /* R */ info => Enumerable.Range(0, 25).All(cell => info[cell] % 8 != 0 || info[cell] / 8 == 2),
        /* S */ info => Enumerable.Range(0, 8).Count(fill => info.Count(s => s % 8 == fill) == 2) == 4,
        /* T */ info => Enumerable.Range(0, 25).All(cell => info[cell] % 8 != 6 || Orthogonal(cell).Any(c => info[c] % 8 == 6)),
        /* U */ info => Enumerable.Range(0, 2).All(row => Enumerable.Range(0, 5).All(col => info[col + 5 * row] % 8 != 5)) || Enumerable.Range(3, 2).All(row => Enumerable.Range(0, 5).All(col => info[col + 5 * row] % 8 != 5)),
        /* V */ info => Enumerable.Range(0, 25).All(cell => cell / 5 == 2 || (cell / 5 < 2 ? info[cell] % 8 != 0 : info[cell] % 8 != 1)) || Enumerable.Range(0, 25).All(cell => cell / 5 == 2 || (cell / 5 < 2 ? info[cell] % 8 != 1 : info[cell] % 8 != 0)),
        /* W */ info => new[] { 6, 7, 8, 11, 12, 13, 16, 17, 18 }.All(cell => info[cell] % 8 != 2),
        /* X */ info => new[] { 0, 1, 2, 3, 4, 5, 9, 10, 14, 15, 19, 20, 21, 22, 23, 24 }.All(cell => info[cell] % 8 != 4),
        /* Y */ info => Enumerable.Range(0, 25).Count(cell => info[cell] % 8 == 1) == Enumerable.Range(0, 25).Where(cell => info[cell] % 8 == 1).Select(cell => info[cell] / 8).Distinct().Count(),
        /* Z */ info => new[] { 5, 6, 7, 8, 9, 15, 16, 17, 18, 19 }.Select(cell => info[cell] % 8).Distinct().Count() == 8,
        /* extra restriction */ info => Enumerable.Range(0, 8).All(value => Enumerable.Range(0, 25).Count(cell => info[cell] / 8 == value) >= 2 && Enumerable.Range(0, 25).Count(cell => info[cell] % 8 == value) >= 2));

#pragma warning disable 414
    private const string TwitchHelpMessage = @"!{0} A1 C E [Place circle-empty at A1.] | Shapes are: [C]ircle, [D]iamond, [H]eart, [O]ctagon, [S]quare, sta[R], trape[Z]oid, [T]riangle. | Fills are: [C]ross, dia[G]onal, [D]ots, [E]mpty, [F]illed, [H]orizotal, [V]ertical, [X].";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        var m = Regex.Match(command, @"^\s*(?<COORDLET>[ABCDE])(?<COORDNUM>[12345])\s*(?<SHAPE>[CDHOSRZT])\s*(?<FILL>[CGDEFHVX])\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (m.Success)
        {
            yield return null;
            var chordLet = m.Groups["COORDLET"].Value.ToUpperInvariant();
            var chordNum = m.Groups["COORDNUM"].Value.ToUpperInvariant();
            int val = chordLet[0] - 'A' + (chordNum[0] - '0' - 1) * 5;
            //Debug.Log(val);

            var shapeNames = "CDHOSRZT";
            var fillNames = "CGDEFHVX";

            var shape = m.Groups["SHAPE"].Value.ToUpperInvariant();
            var fill = m.Groups["FILL"].Value.ToUpperInvariant();
            var finalShape = shapeNames.IndexOf(shape);
            var finalFill = fillNames.IndexOf(fill);
            Debug.LogFormat(finalShape + ", " + finalFill);

            while (CurrentShapeInfo[val] / 8 != finalShape)
            {
                if (_toggleIsFill)
                {
                    ToggleSel.OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
                SquareSels[val].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            while (CurrentShapeInfo[val] % 8 != finalFill)
            {
                if (!_toggleIsFill)
                {
                    ToggleSel.OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
                SquareSels[val].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
        }
        m = Regex.Match(command, @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (m.Success)
        {
            yield return null;
            SubmitSel.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        m = Regex.Match(command, @"^\s*reset\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (m.Success)
        {
            yield return null;
            ResetSel.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        yield break;
    }
}
