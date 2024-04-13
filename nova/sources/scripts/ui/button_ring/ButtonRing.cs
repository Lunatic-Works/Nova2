using System;
using System.Collections.Generic;
using Godot;

namespace Nova;

public readonly struct ButtonRingItem(string actionI18nName, CompressedTexture2D activeSprite)
{
    public readonly CompressedTexture2D ActiveSprite = activeSprite;
    public readonly string ActionI18nName = actionI18nName;
}

public partial class ButtonRing : Control
{
    [Export]
    private TextureRect _ringBackground;
    [Export]
    private Label _actionNameText;
    [Export]
    private float _innerRatio = 0.3f;
    [Export]
    private float _angleOffset = 67.5f;
    [Export]
    private string _textureFolder = "res://resources/button_ring";

    private List<ButtonRingItem> _sectors = [];
    private float _sectorRadius = 200.0f;
    private Vector2 _preCalculatedAnchorPos;
    private int _selectedSectorIndex = -1;

    public override void _EnterTree()
    {
        // The angle of the godot is 0 degrees on the positive x-axis,
        // Clockwise is positive, counterclockwise is negative.
        // In order to adjust the display of the button ring according to the sector index,
        // We have adjusted the order in which the sectors are added.
        _sectors.Add(new(" ", GD.Load<CompressedTexture2D>($"{_textureFolder}/button_ring_0.png")));
        for (var i = 8; i >= 1; i--)
        {
            var texture = GD.Load<CompressedTexture2D>($"{_textureFolder}/button_ring_{i}.png");
            _sectors.Add(new(i.ToString(), texture));
        }
    }

    public override void _Ready()
    {
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
    public float CalculatePointerAngle(Vector2 pointerPos, out float distance)
    {
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

    public override void _Input(InputEvent @event)
    {
        if (!Visible)
        {
            return;
        }
        if (@event is InputEventMouseMotion mouseMotion)
        {
            var angle = CalculatePointerAngle(mouseMotion.Position, out var distance);
            if (float.IsNaN(angle)) return;

            if (distance < _innerRatio * _sectorRadius)
            {
                _selectedSectorIndex = -1;
                _actionNameText.Text = "";
            }
            else
            {
                _selectedSectorIndex = GetSectorIndexAtAngle(angle + _angleOffset);
            }

            SwitchSector(_selectedSectorIndex + 1);
        }
    }
}
