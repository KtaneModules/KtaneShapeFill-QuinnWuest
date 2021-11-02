using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KModkit;

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

        bool[] shapeChecks = new bool[8];
        bool[] fillChecks = new bool[8];
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
            restr2 = (restr2 + 3) % 26;
        _restrictions.Add(restr2);
        var restr3 = restrIx[_fillRestrictions[1]][SerialNumber[2]];
        while (_restrictions.Contains(restr3))
            restr3 = (restr3 + 3) % 26;
        _restrictions.Add(restr3);
        var restr4 = restrIx[_fillRestrictions[0]][SerialNumber[3]];
        while (_restrictions.Contains(restr4))
            restr4 = (restr4 + 3) % 26;
        _restrictions.Add(restr4);
        _restrictions.Add(26);
    }

    private bool TogglePress()
    {
        Debug.LogFormat("[Shape Fill #{0}] Toggle press.", _moduleId);
        _toggleIsFill = !_toggleIsFill;
        ToggleText.text = _toggleIsFill ? "FILL" : "SHAPE";
        return false;
    }

    private bool ResetPress()
    {
        Debug.LogFormat("[Shape Fill #{0}] Reset press.", _moduleId);
        for (int i = 0; i < 25; i++)
        {
            CurrentShapeInfo[i] = InitialShapeInfo[i];
            ShapeFillObjs[i].GetComponent<MeshRenderer>().material.mainTexture = ShapeTextures[InitialShapeInfo[i]];
        }
        Light.GetComponent<MeshRenderer>().material = LightMats[0];
        return false;
    }

    private KMSelectable.OnInteractHandler SquarePress(int shape)
    {
        return delegate ()
        {
            Light.GetComponent<MeshRenderer>().material = LightMats[3];
            if (_toggleIsFill)
                CurrentShapeInfo[shape] = ((CurrentShapeInfo[shape] / 8) * 8) + (((CurrentShapeInfo[shape] % 8) + 1) % 8);
            if (!_toggleIsFill)
                CurrentShapeInfo[shape] = (CurrentShapeInfo[shape] + 8) % 64;
            ShapeFillObjs[shape].GetComponent<MeshRenderer>().material.mainTexture = ShapeTextures[CurrentShapeInfo[shape]];
            return false;
        };
    }

    private bool SubmitPress()
    {
        Debug.LogFormat("[Shape Fill #{0}] Submit press.", _moduleId);
        bool satisfied = true;
        return false;
    }

    private static T[] arrthing<T>(params T[] array) { return array; }

    private static readonly Func<int[], bool>[] _restDelegates = arrthing<Func<int[], bool>>(
        info => // A
        {
            int[] ix = { 6, 7, 8, 11, 12, 13, 16, 17, 18 };
            for (int i = 0; i < 9; i++)
                if (info[ix[i]] / 8 == 6)
                    return false;
            return true;
        },
        info => // B
        {
            return Enumerable.Range(0, 8).Count(shape => info.Count(s => s / 8 == shape) == 2) == 4;
        },
        info => // C
        {
            for (int i = 0; i < 25; i++)
            {
                if (info[i] / 8 == 7)
                {
                    List<int> adj = new List<int>();
                    if (i % 5 != 0)
                        adj.Add(info[i - 1] / 8);
                    if (i % 5 != 4)
                        adj.Add(info[i + 1] / 8);
                    if (i / 5 != 0)
                        adj.Add(info[i - 5] / 8);
                    if (i / 5 != 4)
                        adj.Add(info[i + 5] / 8);
                    if (!adj.Contains(7))
                        return false;
                }
            }
            return true;
        },
        info => // D
        {
            int[] ix = arrthing(info[0] / 8, info[5] / 8, info[10] / 8, info[15] / 8, info[20] / 8, info[4] / 8, info[9] / 8, info[14] / 8, info[19] / 8, info[24] / 8);
            return ix.Contains(0) && ix.Contains(1) && ix.Contains(2) && ix.Contains(3) && ix.Contains(4) && ix.Contains(5) && ix.Contains(6) && ix.Contains(7);
        },
        info => // E
        {

        }
    );
}
