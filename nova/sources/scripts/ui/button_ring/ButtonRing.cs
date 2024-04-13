using System;
using System.Collections.Generic;
using Godot;

namespace Nova;

public struct ButtonRingItem(string actionI18nName, CompressedTexture2D activeSprite)
{
    public CompressedTexture2D ActiveSprite = activeSprite;
    public string ActionI18nName = actionI18nName;
}

public partial class ButtonRing : Control
{
    private List<ButtonRingItem> _sectors = [];
    private float _sectorRadius = 200.0f;
    public float InnerRatio = 0.3f;
    public float AngleOffset = -67.5f;
    private Vector2 _preCalculatedAnchorPos;
    private int _selectedSectorIndex = -1;
    private TextureRect _ringBackground;
    private Label _actionNameText;


    public override void _Ready()
    {
        _ringBackground = GetNode<TextureRect>("TextureRect");
        _actionNameText = GetNode<Label>("Label");

        Init();
    }

    private void Init()
    {
        GD.Print("Start Init: ButtonRing");
        var spritePath = "res://resources/button_ring/";

        // The angle of the godot is 0 degrees on the positive x-axis,
        // Clockwise is positive, counterclockwise is negative.
        // In order to adjust the display of the button ring according to the sector index,
        // We have adjusted the order in which the sectors are added. 
        _sectors.Add(new(" ", GD.Load<CompressedTexture2D>(spritePath + "button_ring_0.png")));
        _sectors.Add(new("7", GD.Load<CompressedTexture2D>(spritePath + "button_ring_7.png")));
        _sectors.Add(new("6", GD.Load<CompressedTexture2D>(spritePath + "button_ring_6.png")));
        _sectors.Add(new("5", GD.Load<CompressedTexture2D>(spritePath + "button_ring_5.png")));
        _sectors.Add(new("4", GD.Load<CompressedTexture2D>(spritePath + "button_ring_4.png")));
        _sectors.Add(new("3", GD.Load<CompressedTexture2D>(spritePath + "button_ring_3.png")));
        _sectors.Add(new("2", GD.Load<CompressedTexture2D>(spritePath + "button_ring_2.png")));
        _sectors.Add(new("1", GD.Load<CompressedTexture2D>(spritePath + "button_ring_1.png")));
        _sectors.Add(new("8", GD.Load<CompressedTexture2D>(spritePath + "button_ring_8.png")));

        SwitchSector(0);
    }

    private void SwitchSector(int index)
    {
        if (index < 0 || index >= _sectors.Count) return;

        _ringBackground.Texture = _sectors[index].ActiveSprite;
        _actionNameText.Text = _sectors[index].ActionI18nName;
    }


    /// <summary>
    /// return relative angle in deg, range from [0, 360), distance will be the distance from pointer to anchor point
    /// </summary>
    /// <returns>return relative angle in deg, range from [0, 360)</returns>
    public float CalculatePointerRelative(out float distance)
    {
        var pointerPos = RealInput.PointerPosition;
        // var anchorPos = preCalculatedAnchorPos;
        var anchorPos = Position + PivotOffset;
        var diff = pointerPos - anchorPos;
        distance = (float)Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y);
        var angle = Mathf.Atan2(diff.Y, diff.X);
        if (angle < 0f)
        {
            // angle ranges in [0, 2 PI)
            angle = Mathf.Pi * 2f + angle;
        }

        // cvt to reg in [0, 360)
        angle = Mathf.RadToDeg(angle);

        return angle;
    }

    /// <summary>
    /// Relative angle between [0, 360)
    /// </summary>
    /// <param name="angle">relative angle</param>
    /// <returns>the index of sector being hovered. if no sector is hovered, return -1</returns>
    private int GetSectorIndexAtAngle(float angle)
    {
        var sectorRange = 360f / 8;
        var index = (int)(angle / sectorRange);
        if (index < 0)
        {
            index += _sectors.Count;
        }

        return index;
    }

    public override void _Process(double delta)
    {
        var pointerRelativeAngle = CalculatePointerRelative(out var distance);
        if (float.IsNaN(pointerRelativeAngle)) return;

        if (distance < InnerRatio * _sectorRadius)
        {
            _selectedSectorIndex = -1;
            _actionNameText.Text = "";
        }
        else
        {
            _selectedSectorIndex = GetSectorIndexAtAngle(pointerRelativeAngle);
        }

        SwitchSector(_selectedSectorIndex + 1);
    }
}