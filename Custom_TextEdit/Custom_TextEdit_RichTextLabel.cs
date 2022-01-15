using Godot;
using System;

public class Custom_TextEdit_RichTextLabel : HBoxContainer
{
    
    public Custom_TextEdit CustomTextEdit;

    [Export]
    public Font EditorFont;

    [Export]
    public Color SelectionColor = Colors.Yellow;
    [Export]
    public Color CursorSelectionColor = Colors.SkyBlue * new Color(1,1,1,0.25f);

    public bool IsSelected = false;
    public int SelectionPosition = 0;

    public Label LineNumberLabel = null;
    public RichTextLabel RichLabel = null;

    public String LineText = "";
    public int LineNumber = 0;

    // Variables for syntax highlighting
    // =========================
    public bool ApplySyntaxHighlight = true;
    private string _SyntaxCurrentWord = "";
    private string _SyntaxCurrentWordOutput = "";

    private bool _SyntaxRegionIsCurrent = false;
    private string _SyntaxRegionEndWord = "";
    private string _SyntaxRegionEndCode = "";
    
    private char _SyntaxCharLastChar = '\n';
    private string _SyntaxCharOutput = "";
    private bool _SyntaxCharRegionIsCurrent = false;
    private char _SyntaxCharRegionEndChar = '\n';
    private string _SyntaxCharRegionEndCode = "";
    // =========================

    public override void _Ready()
    {
        RichLabel = GetNodeOrNull<RichTextLabel>("RichTextLabel");
        LineNumberLabel = GetNodeOrNull<Label>("LineLabel");
        UpdateLineText();

        // Save performance - disable unnecessary calls
        SetProcess(false);
        SetPhysicsProcess(false);
        SetProcessInput(false);
        SetProcessUnhandledInput(false);
    }

    public void SetLineText(String newText)
    {
        LineText = newText;

        // TODO - look at enabling word wrap support.
        Vector2 NewMinSize = EditorFont.GetStringSize(newText);
        // Add some padding if the scroll bar(s) are visible
        NewMinSize.x += EditorFont.GetCharSize('a').x * 6.0f;
        // Assign
        RectMinSize = NewMinSize;

        UpdateLineText();
    }
    public void SetLineNumber(int newLineNumber, int newLineMaximumCount=0)
    {
        LineNumber = newLineNumber;
        if (LineNumberLabel != null)
        {
            LineNumberLabel.Text = LineNumber.ToString() + ":";
        }

        if (newLineMaximumCount > 0)
        {
            LineNumberLabel.RectMinSize = EditorFont.GetStringSize(newLineMaximumCount.ToString() + ": ");
        }
    }

    public Vector2 GetWordWrapSize(String textToProcess)
    {
        // TODO - make this "smart" by making it per-word rather than per character
        // Which will (potentially) allow word-wrapping.
        Vector2 drawRectSize = Vector2.Zero;
        Vector2 drawRectCharSize = Vector2.Zero;
        for (int i = 0; i < textToProcess.Length; i++)
        {
            drawRectCharSize = EditorFont.GetCharSize(textToProcess[i]);
            drawRectSize.x += drawRectCharSize.x;
            if (drawRectSize.x >= RichLabel.RectSize.x)
            {
                drawRectSize.y += drawRectCharSize.y;
                drawRectSize.x = drawRectCharSize.x;
            }
        }
        if (drawRectSize.y > 0)
        {
            drawRectSize.x = RichLabel.RectSize.x;
            drawRectSize.y += drawRectCharSize.y;
        }
        else
        {
            drawRectSize.y = drawRectCharSize.y;
        }

        return drawRectSize;
    }
    public Vector2 GetWordWrapEndPos(String textToProcess, int textDesiredPos)
    {
        // TODO - make this "smart" by making it per-word rather than per character
        // Which will (potentially) allow word-wrapping.
        Vector2 draw_rect_pos = new Vector2();
        Vector2 draw_rect_select_char_size = Vector2.Zero;

        // Get the character position
        for (int i = 0; i < textDesiredPos; i++)
        {
            if (i > textToProcess.Length-1)
            {
                // Return as close as possible
                return draw_rect_pos;
            }
            
            draw_rect_select_char_size = EditorFont.GetCharSize(textToProcess[i]);
            draw_rect_pos.x += draw_rect_select_char_size.x;
            if (draw_rect_pos.x >= RichLabel.RectSize.x)
            {
                draw_rect_pos.y += draw_rect_select_char_size.y;
                draw_rect_pos.x = draw_rect_select_char_size.x;
            }
        }
        return draw_rect_pos;
    }

    private void UpdateLineText()
    {
        if (RichLabel != null)
        {
            if (ApplySyntaxHighlight == true)
            {
                UpdateLineText_SyntaxHighlight();
            }
            else
            {
                RichLabel.BbcodeText = LineText;
            }
        }
        if (LineNumberLabel != null)
        {
            LineNumberLabel.Text = LineNumber.ToString() + ":";
        }
    }
    private void UpdateLineText_SyntaxHighlight()
    {
        String bbcode = "";
        
        _SyntaxCurrentWord = "";
        _SyntaxRegionIsCurrent = false;
        _SyntaxCharRegionIsCurrent = false;

        for (int i = 0; i < LineText.Length; i++)
        {
            // If we are not looking for a region
            if (_SyntaxRegionIsCurrent == false && _SyntaxCharRegionIsCurrent == false)
            {
                // Process the character (just in case it's a keyword)
                _SyntaxCharLastChar = LineText[i];
                if (SyntaxProcessChar() == true)
                {
                    // process the word IF we are not in a char region
                    if (_SyntaxCharRegionIsCurrent == false)
                    {
                        SyntaxProcessWord();
                        bbcode += _SyntaxCurrentWordOutput;
                    }
                    else
                    {
                        bbcode += _SyntaxCurrentWord;
                    }

                    // Add the character
                    bbcode += _SyntaxCharOutput;

                    // Not sure if this is right...
                    _SyntaxCurrentWord = "";
                }
                else
                {
                    if (CustomTextEdit.KeywordWordEndSymbols.Contains(LineText[i]) == true)
                    {
                        // Process the syntax on the current word
                        SyntaxProcessWord();
                        bbcode += _SyntaxCurrentWordOutput;
                        
                        // Add the symbol
                        //bbcode += line_text[i];
                        _SyntaxCharLastChar = LineText[i];
                        if (SyntaxProcessChar() == true)
                        {
                            // Add the character with highlights
                            bbcode += _SyntaxCharOutput;
                        }
                        else
                        {
                            bbcode += LineText[i];
                        }

                        _SyntaxCurrentWord = "";
                    }
                    else
                    {
                        // Add the character
                        _SyntaxCurrentWord += LineText[i];
                    }
                }
            }
            else if (_SyntaxRegionIsCurrent == true)
            {
                if (CustomTextEdit.KeywordWordEndSymbols.Contains(LineText[i]) == true)
                {
                    // Add the word normally
                    bbcode += _SyntaxCurrentWord;

                    // Is it the end of the region?
                    if (_SyntaxCurrentWord == _SyntaxRegionEndWord)
                    {
                        _SyntaxRegionIsCurrent = false;
                        bbcode += _SyntaxRegionEndCode;
                    }

                    // Add the symbol
                    bbcode += LineText[i];

                    _SyntaxCurrentWord = "";
                }
                else
                {
                    // Add the character
                    _SyntaxCurrentWord += LineText[i];
                }
            }
            else if (_SyntaxCharRegionIsCurrent == true)
            {
                // Not totally sure if this will work TBH
                if (LineText[i] == _SyntaxCharRegionEndChar)
                {
                    bbcode += _SyntaxCharRegionEndChar;
                    bbcode += _SyntaxCharRegionEndCode;
                    _SyntaxCharRegionIsCurrent = false;
                }
                else
                {
                    bbcode += LineText[i];
                }
            }
        }
        if (_SyntaxCharRegionIsCurrent == true)
        {
            bbcode += _SyntaxCharRegionEndCode;
            _SyntaxCurrentWord = "";
        }
        if (_SyntaxCurrentWord != "")
        {
            SyntaxProcessWord();
            bbcode += _SyntaxCurrentWordOutput;
        }
        if (_SyntaxRegionIsCurrent == true)
        {
            bbcode += _SyntaxRegionEndCode;
        }

        RichLabel.BbcodeText = bbcode;
    }
    private bool SyntaxProcessWord()
    {
        // Do we have a region? If so, then skip
        if (_SyntaxRegionIsCurrent == true || _SyntaxCharRegionIsCurrent == true)
        {
            _SyntaxCurrentWordOutput = _SyntaxCurrentWord;
            return false;
        }

        if (CustomTextEdit.KeywordRegionColors.ContainsKey(_SyntaxCurrentWord) == true)
        {
            _SyntaxCurrentWordOutput = "[color=#" + CustomTextEdit.KeywordRegionColors[_SyntaxCurrentWord].RegionColor.ToHtml() + "]" + _SyntaxCurrentWord;
            _SyntaxRegionEndWord = CustomTextEdit.KeywordRegionColors[_SyntaxCurrentWord].RegionEnd;
            _SyntaxRegionEndCode = "[/color]";
            _SyntaxRegionIsCurrent = true;
            return true;
        }
        else if (CustomTextEdit.KeywordColors.ContainsKey(_SyntaxCurrentWord) == true)
        {
            _SyntaxCurrentWordOutput = "[color=#" + CustomTextEdit.KeywordColors[_SyntaxCurrentWord].ToHtml() + "]" + _SyntaxCurrentWord + "[/color]";
            return true;
        }
        else
        {
            _SyntaxCurrentWordOutput = _SyntaxCurrentWord;
            return false;
        }
    }
    private bool SyntaxProcessChar()
    {
        if (CustomTextEdit.KeywordCharRegionColors.ContainsKey(_SyntaxCharLastChar) == true)
        {
            SyntaxInfoCharRegion charRegion = CustomTextEdit.KeywordCharRegionColors[_SyntaxCharLastChar];

            // If it requires a new word and we are NOT a new word, then skip
            if (charRegion.HasToBeWordStart == true && _SyntaxCurrentWord != "")
            {
                _SyntaxCharOutput = "" + _SyntaxCharLastChar;
                return false;
            }

            _SyntaxCharOutput = "[color=#" + charRegion.RegionColor.ToHtml() + "]" + _SyntaxCharLastChar;
            if (charRegion.RegionEnabled == true)
            {
                _SyntaxCharRegionIsCurrent = true;
                _SyntaxCharRegionEndChar = charRegion.RegionEnd;
                _SyntaxCharRegionEndCode = "[/color]";
            }
            else
            {
                _SyntaxCharOutput += "[/color]";
            }
            return true;
        }
        else
        {
            _SyntaxCharOutput = "" + _SyntaxCharLastChar;
            return false;
        }
    }


    public void UpdateSelected(bool p_IsSelected, int p_SelectionPosition)
    {
        IsSelected = p_IsSelected;
        SelectionPosition = p_SelectionPosition;
        Update();
    }

    public override void _Draw()
    {
        base._Draw();

        // Draw the cursor selection stuff
        // ==========
        // Should we be highlighted?
        if (LineNumber >= CustomTextEdit.CursorSelectionLineStart && LineNumber <= CustomTextEdit.CursorSelectionLineEnd)
        {
            // Should we just highlight all text?
            if (LineNumber != CustomTextEdit.CursorSelectionLineStart && LineNumber != CustomTextEdit.CursorSelectionLineEnd)
            {
                Vector2 StringSize = EditorFont.GetStringSize(LineText);
                Vector2 lineOffset = RichLabel.RectPosition;
                lineOffset.y = 0;
                DrawRect(new Rect2(lineOffset, StringSize), CursorSelectionColor);
            }
            else
            {
                // Are we the start or the end?
                if (LineNumber == CustomTextEdit.CursorSelectionLineStart)
                {
                    // Is the selection on just this line?
                    if (LineNumber == CustomTextEdit.CursorSelectionLineEnd)
                    {
                        String PreString = "";
                        for (int i = 0; i < CustomTextEdit.CursorSelectionPositionStart; i++)
                        {
                            PreString += LineText[i];
                        }
                        String DesiredString = "";
                        for (int i = CustomTextEdit.CursorSelectionPositionStart; i < CustomTextEdit.CursorSelectionPositionEnd; i++)
                        {
                            DesiredString += LineText[i];
                        }

                        Vector2 StringSize = EditorFont.GetStringSize(DesiredString);
                        Vector2 lineOffset = RichLabel.RectPosition;
                        lineOffset.y = 0;
                        // Offset
                        lineOffset.x += EditorFont.GetStringSize(PreString).x;

                        DrawRect(new Rect2(lineOffset, StringSize), CursorSelectionColor);
                    }
                    // Otherwise We just need to highlight from us to the end
                    else
                    {
                        String PreString = "";
                        for (int i = 0; i < Mathf.Min(CustomTextEdit.CursorSelectionPositionStart, LineText.Length); i++)
                        {
                            PreString += LineText[i];
                        }
                        String DesiredString = "";
                        for (int i = CustomTextEdit.CursorSelectionPositionStart; i < LineText.Length; i++)
                        {
                            DesiredString += LineText[i];
                        }

                        Vector2 StringSize = EditorFont.GetStringSize(DesiredString);
                        Vector2 lineOffset = RichLabel.RectPosition;
                        lineOffset.y = 0;
                        // Offset
                        lineOffset.x += EditorFont.GetStringSize(PreString).x;

                        DrawRect(new Rect2(lineOffset, StringSize), CursorSelectionColor);
                    }
                }
                // Just highlight from the beginning to the end
                else
                {
                    String DesiredString = "";
                    for (int i = 0; i < Mathf.Min(CustomTextEdit.CursorSelectionPositionEnd, LineText.Length); i++)
                    {
                        DesiredString += LineText[i];
                    }

                    Vector2 StringSize = EditorFont.GetStringSize(DesiredString);
                    Vector2 lineOffset = RichLabel.RectPosition;
                    lineOffset.y = 0;

                    DrawRect(new Rect2(lineOffset, StringSize), CursorSelectionColor);
                }
            }
        }
        // ==========

        // Draw the cursor caret
        // ==========
        if (IsSelected == true)
        {
            String textToCursorPos = "";
            for (int i = 0; i < SelectionPosition; i++)
            {
                textToCursorPos += LineText[i];
            }
            Vector2 textToCursorSize = EditorFont.GetStringSize(textToCursorPos);
            
            Rect2 newRect = new Rect2(RichLabel.RectPosition + textToCursorSize, new Vector2(2, textToCursorSize.y));
            newRect.Position *= new Vector2(1, 0);

            DrawRect(newRect, SelectionColor);
        }
        // ==========
    }
}
