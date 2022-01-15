using Godot;
using System;
using System.Collections.Generic;

public struct SyntaxInfoRegion
{
    public String RegionEnd;
    public Color RegionColor;
    public SyntaxInfoRegion(String end, Color col)
    {
        RegionEnd = end;
        RegionColor = col;
    }
}
public struct SyntaxInfoCharRegion
{
    public char RegionEnd;
    public bool RegionEnabled;
    public bool HasToBeWordStart;
    public Color RegionColor;
    public SyntaxInfoCharRegion(char end, bool endEnabled, bool hasStart, Color col)
    {
        RegionEnd = end;
        RegionEnabled = endEnabled;
        HasToBeWordStart = hasStart;
        RegionColor = col;
    }
}

public class Custom_TextEdit : Control
{

    // For testing
    [Export(PropertyHint.MultilineText)]
    public String EditorText = "";

    [Export]
    public bool AutoApplyDefaultSyntaxHighlighting = true;

    [Export]
    public PackedScene EditorLine = null;

    [Export]
    public Font EditorFont;


    public List<Custom_TextEdit_RichTextLabel> FileLabels = new List<Custom_TextEdit_RichTextLabel>();


    public VBoxContainer LineContainer;


    public int CursorLine = 0;
    public int CursorPosition = 0;
    private Custom_TextEdit_RichTextLabel LastSelectedLabel = null;

    public int CursorSelectionLineStart = 1;
    public int CursorSelectionLineEnd = 4;
    public int CursorSelectionPositionStart = 2;
    public int CursorSelectionPositionEnd = 10;

    // Movement operations for selecting modify this, which in turn is used to update the cursor_selection_xxx variables
    private int VirtualCursorSelectionLine = 0;
    private int VirtualCursorSelectionPosition = 0;

    public Dictionary<String, Color> KeywordColors = new Dictionary<String, Color>();
    public Dictionary<String, SyntaxInfoRegion> KeywordRegionColors = new Dictionary<string, SyntaxInfoRegion>();
    public Dictionary<char, SyntaxInfoCharRegion> KeywordCharRegionColors = new Dictionary<char, SyntaxInfoCharRegion>();
    public HashSet<char> KeywordWordEndSymbols = new HashSet<char>();


    public override void _Ready()
    {
        LineContainer = GetNodeOrNull<VBoxContainer>("ScrollContainer/VBoxContainer");
        if (AutoApplyDefaultSyntaxHighlighting == true)
        {
            AddDefaultSyntaxColoring();
        }
        RefreshAllLines();
    }

    public void AddDefaultSyntaxColoring()
    {
        // Example coloring based on New Moon syntax
        // https://taniarascia.github.io/new-moon/

        // Keywords
        Color KeywordColor = new Color("ffeea6");
        AddKeywordColor("var", KeywordColor);
        AddKeywordColor("func", KeywordColor);
        AddKeywordColor("if", KeywordColor);
        AddKeywordColor("elif", KeywordColor);
        AddKeywordColor("else", KeywordColor);
        AddKeywordColor("const", KeywordColor);
        AddKeywordColor("class_name", KeywordColor);
        AddKeywordColor("extends", KeywordColor);
        AddKeywordColor("onready", KeywordColor);
        AddKeywordColor("for", KeywordColor);

        // Support
        Color SupportColor = new Color("e1a6f2");
        AddKeywordColor("null", SupportColor);
        AddKeywordColor("true", SupportColor);
        AddKeywordColor("false", SupportColor);

        // Numbers
        AddKeywordCharRegionColor('1', SupportColor, '\n', false, true);
        AddKeywordCharRegionColor('2', SupportColor, '\n', false, true);
        AddKeywordCharRegionColor('3', SupportColor, '\n', false, true);
        AddKeywordCharRegionColor('4', SupportColor, '\n', false, true);
        AddKeywordCharRegionColor('5', SupportColor, '\n', false, true);
        AddKeywordCharRegionColor('6', SupportColor, '\n', false, true);
        AddKeywordCharRegionColor('7', SupportColor, '\n', false, true);
        AddKeywordCharRegionColor('8', SupportColor, '\n', false, true);
        AddKeywordCharRegionColor('9', SupportColor, '\n', false, true);
        AddKeywordCharRegionColor('0', SupportColor, '\n', false, true);

        // Operators
        Color OperatorColor = new Color("ac8d58");
        AddKeywordCharRegionColor('+', OperatorColor);
        AddKeywordCharRegionColor('-', OperatorColor);
        AddKeywordCharRegionColor('*', OperatorColor);
        AddKeywordCharRegionColor('=', OperatorColor);
        AddKeywordCharRegionColor('<', OperatorColor);
        AddKeywordCharRegionColor('>', OperatorColor);
        AddKeywordCharRegionColor('.', OperatorColor);
        AddKeywordCharRegionColor('!', OperatorColor);
        AddKeywordCharRegionColor('|', OperatorColor);
        // Characters that are like operators
        AddKeywordCharRegionColor(';', OperatorColor);
        AddKeywordCharRegionColor(':', OperatorColor);
        AddKeywordCharRegionColor('(', OperatorColor);
        AddKeywordCharRegionColor(')', OperatorColor);
        AddKeywordCharRegionColor('[', OperatorColor);
        AddKeywordCharRegionColor(']', OperatorColor);
        AddKeywordCharRegionColor('{', OperatorColor);
        AddKeywordCharRegionColor('}', OperatorColor);

        // All C# Godot classes
        Color ClassColor = new Color("6AB0F3");
        Type[] GodotTypes = typeof(Node).Assembly.GetTypes();
        foreach (Type tmp in GodotTypes)
        {
            AddKeywordColor(tmp.Name, ClassColor);
        }

        // Comments
        Color CommentColor = new Color("777c85");
        AddKeywordRegionColor("//", CommentColor);
        AddKeywordRegionColor("/*", CommentColor, "*/");
        AddKeywordRegionColor("#", CommentColor);

        // String
        Color StringColor = new Color("92d192");
        AddKeywordCharRegionColor('"', StringColor, '"', true, false);
        // $ for getNode
        AddKeywordCharRegionColor('$', StringColor, ' ', true, false);

        // Splitting characters
        AddKeywordEndSymbol(' ');
        AddKeywordEndSymbol('.');
        AddKeywordEndSymbol('\t');
        AddKeywordEndSymbol('+');
        AddKeywordEndSymbol('-');
        AddKeywordEndSymbol('=');
        AddKeywordEndSymbol('<');
        AddKeywordEndSymbol('>');
        AddKeywordEndSymbol('!');
        AddKeywordEndSymbol('|');
        AddKeywordEndSymbol(';');
        AddKeywordEndSymbol(':');
        AddKeywordEndSymbol('(');
        AddKeywordEndSymbol(')');
        AddKeywordEndSymbol('[');
        AddKeywordEndSymbol(']');
        AddKeywordEndSymbol('{');
        AddKeywordEndSymbol('}');
        AddKeywordEndSymbol('\n'); // shouldn't occur.
    }

    public void AddKeywordColor(String keyword, Color color)
    {
        if (KeywordColors.ContainsKey(keyword) == false)
        {
            KeywordColors.Add(keyword, color);
        }
        else
        {
            KeywordColors[keyword] = color;
        }
    }
    public void AddKeywordRegionColor(String keyword, Color color, String endKeyword="\n")
    {
        if (KeywordRegionColors.ContainsKey(keyword) == false)
        {
            KeywordRegionColors.Add(keyword, new SyntaxInfoRegion(endKeyword, color));
        }
        else
        {
            KeywordRegionColors[keyword] = new SyntaxInfoRegion(endKeyword, color);
        }
    }
    public void AddKeywordCharRegionColor(char keyword, Color color, char endChar='\n', bool useEndChar=false, bool hasToBeWordStart=false)
    {
        if (KeywordCharRegionColors.ContainsKey(keyword) == false)
        {
            KeywordCharRegionColors.Add(keyword, new SyntaxInfoCharRegion(endChar, useEndChar, hasToBeWordStart, color));
        }
        else
        {
            KeywordCharRegionColors[keyword] = new SyntaxInfoCharRegion(endChar, useEndChar, hasToBeWordStart, color);
        }
    }
    public void AddKeywordEndSymbol(char keywordSymbol)
    {
        KeywordWordEndSymbols.Add(keywordSymbol);
    }

    public void RefreshAllLines()
    {
        for (int i = 0; i < FileLabels.Count; i++)
        {
            FileLabels[i].QueueFree();
        }
        FileLabels.Clear();

        LastSelectedLabel = null;

        // Add the lines
        int lineCount = 0;
        String lineValue = "";
        for (int charIndex = 0; charIndex < EditorText.Length; charIndex++)
        {
            if (EditorText[charIndex] == '\n')
            {
                RefreshAllLines_CreateLine(lineCount, lineValue);
                lineCount += 1;
                lineValue = "";
            }
            else
            {
                lineValue += EditorText[charIndex];
            }
        }
        if (lineValue != "")
        {
            RefreshAllLines_CreateLine(lineCount, lineValue);
            lineCount += 1;
            lineValue = "";
        }

        // If there are no lines, then just add a single line
        if (lineCount == 0)
        {
            RefreshAllLines_CreateLine(lineCount, "");
            lineCount += 1;
            lineValue = "";
        }

        // Keep in bounds
        if (lineCount > 0)
        {
            CursorLine = Mathf.Clamp(CursorLine, 0, FileLabels.Count-1);
            CursorPosition = Mathf.Clamp(CursorPosition, 0, FileLabels[CursorLine].LineText.Length);
        }
        else
        {
            CursorLine = 0;
            CursorPosition = 0;
        }

        UpdateLineNumbers();

        VirtualCursorSelectionLine = CursorLine;
        VirtualCursorSelectionPosition = CursorPosition;
        UpdateSelectionBasedOnVirtualCursor();
    }
    // Creates a RichTextLabel line and inserts it at the end of the list/array (used for initial creation and/or creation from scratch)
    private void RefreshAllLines_CreateLine(int lineCount, string lineValue)
    {
        Node clone = EditorLine.Instance();
        Custom_TextEdit_RichTextLabel cloneLabel = clone as Custom_TextEdit_RichTextLabel;

        cloneLabel.CustomTextEdit = this;

        cloneLabel.SetLineText(lineValue);

        FileLabels.Add(cloneLabel);
        LineContainer.AddChild(cloneLabel);

        Vector2 labelMinSize = cloneLabel.RectMinSize;
        Vector2 labelSize = EditorFont.GetWordwrapStringSize(lineCount.ToString() + lineValue, LineContainer.RectSize.x);
        labelMinSize.y = labelSize.y;
        cloneLabel.RectMinSize = labelMinSize;
    }
    // Inserts a RichTextLabel line at the given lineIndex
    private void InsertLine(int lineIndex, string lineValue)
    {
        Node clone = EditorLine.Instance();
        Custom_TextEdit_RichTextLabel clone_label = clone as Custom_TextEdit_RichTextLabel;

        clone_label.CustomTextEdit = this;
        FileLabels.Insert(lineIndex, clone_label);
        LineContainer.AddChild(clone_label);
        LineContainer.MoveChild(clone_label, lineIndex);

        clone_label.SetLineText(lineValue);

        Vector2 label_min_size = clone_label.RectMinSize;
        Vector2 label_size = EditorFont.GetWordwrapStringSize((lineIndex+1).ToString() + lineValue, LineContainer.RectSize.x);
        label_min_size.y = label_size.y;
        clone_label.RectMinSize = label_min_size;
    }
    // Updates all the line numbers
    private void UpdateLineNumbers()
    {
        for (int i = 0; i < FileLabels.Count; i++)
        {
            FileLabels[i].SetLineNumber(i, FileLabels.Count);
        }
    }

    public void UpdateEditorTextInternal()
    {
        String new_text = "";
        for (int i = 0; i < FileLabels.Count; i++)
        {
            new_text += FileLabels[i].LineText;
            new_text += "\n";
        }
        EditorText = new_text;
    }

    public override void _Input(InputEvent @event)
    {
        if (Visible == false)
        {
            return;
        }
        // If there is a mouse click
        else if (@event is InputEventMouseButton)
        {
            InputEventMouseButton mouse_event = @event as InputEventMouseButton;
            if (mouse_event.IsPressed() == true && mouse_event.ButtonIndex == ((int)ButtonList.Left) )
            {
                // If it's inside this node
                if (LineContainer.GetGlobalRect().HasPoint(mouse_event.Position) == true)
                {
                    GrabFocus();

                    if (mouse_event.Shift == false)
                    {
                        // Set the cursor position
                        SetCursorToPosition(mouse_event.Position);

                        // Normal cursor move - so undo any selection
                        VirtualCursorSelectionLine = CursorLine;
                        VirtualCursorSelectionPosition = CursorPosition;
                        UpdateSelectionBasedOnVirtualCursor();
                    }
                    else
                    {
                        SetCursorToPosition(mouse_event.Position, true);
                    }
                }
                else
                {
                    ReleaseFocus();
                }
            }
        }
        // If there is a touch
        else if (@event is InputEventScreenTouch)
        {
            InputEventScreenTouch touch_event = @event as InputEventScreenTouch;
            if (touch_event.IsPressed() == true)
            {
                // If it's inside this node
                if (LineContainer.GetGlobalRect().HasPoint(touch_event.Position) == true)
                {
                    GrabFocus();
                    // Set the cursor position
                    SetCursorToPosition(touch_event.Position);

                    // Normal cursor move - so undo any selection
                    VirtualCursorSelectionLine = CursorLine;
                    VirtualCursorSelectionPosition = CursorPosition;
                    UpdateSelectionBasedOnVirtualCursor();
                }
                else
                {
                    ReleaseFocus();
                }
            }
        }
        // Keyboard key input (all the magic happens here!)
        else if (@event is InputEventKey && HasFocus() == true)
        {
            InputEventKey key_press = @event as InputEventKey;
            if (key_press.Pressed == true)
            {
                // Delete text
                if (key_press.Scancode == (int)KeyList.Backspace || key_press.Scancode == (int)KeyList.Delete)
                {
                    if (CursorLine != VirtualCursorSelectionLine || CursorPosition != VirtualCursorSelectionPosition)
                    {
                        TextEditorAction_DeleteText_Selection();
                    }
                    else
                    {
                        TextEditorAction_DeleteText();
                    }
                }
                // Add tabs
                else if (key_press.Scancode == (int)KeyList.Tab)
                {
                    TextEditorAction_AddTab();
                }
                // Paste clipboard text
                else if ( (key_press.Scancode == (int)KeyList.V && key_press.Control == true) || (key_press.Scancode == (int)KeyList.V && key_press.Command == true))
                {
                    // Do we have a selection? If so, delete it first
                    if (CursorLine != VirtualCursorSelectionLine || CursorPosition != VirtualCursorSelectionPosition)
                    {
                        TextEditorAction_DeleteText_Selection();
                    }
                    TextEditorAction_PasteClipboard();
                }
                // Copy clipboard text
                else if ( (key_press.Scancode == (int)KeyList.C && key_press.Control == true) || (key_press.Scancode == (int)KeyList.C && key_press.Command == true))
                {
                    TextEditorAction_CopyClipboard();
                }
                // Cut clipboard text
                else if ( (key_press.Scancode == (int)KeyList.X && key_press.Control == true) || (key_press.Scancode == (int)KeyList.X && key_press.Command == true))
                {
                    TextEditorAction_CopyClipboard();
                    TextEditorAction_DeleteText_Selection();
                }
                // Add a new line
                else if (key_press.Scancode == (int)KeyList.Enter)
                {
                    // Do we have a selection? If so, delete it first
                    if (CursorLine != VirtualCursorSelectionLine || CursorPosition != VirtualCursorSelectionPosition)
                    {
                        TextEditorAction_DeleteText_Selection();
                    }
                    TextEditorAction_AddNewLine();
                }
                // Navigate with arrow keys
                else if (key_press.Scancode == (int)KeyList.Right || key_press.Scancode == (int)KeyList.Left || key_press.Scancode == (int)KeyList.Up || key_press.Scancode == (int)KeyList.Down)
                {
                    if (key_press.Shift == true)
                    {
                        TextEditorAction_NavigateArrowKeys_Selection(key_press);
                    }
                    else
                    {
                        TextEditorAction_NavigateArrowKeys(key_press);
                        // Normal cursor move - so undo any selection
                        VirtualCursorSelectionLine = CursorLine;
                        VirtualCursorSelectionPosition = CursorPosition;
                        UpdateSelectionBasedOnVirtualCursor();
                    }
                }
                // If it is not a special key, then just insert it
                else
                {
                    // Is it a normal key (I.E not a non-visible one)
                    char key_as_code = (char)key_press.Unicode;
                    if (Char.IsControl(key_as_code) == false)
                    {
                        // Do we have a selection? If so, delete it first
                        if (CursorLine != VirtualCursorSelectionLine || CursorPosition != VirtualCursorSelectionPosition)
                        {
                            TextEditorAction_DeleteText_Selection();
                        }

                        TextEditorAction_InsertCharacter(key_press);
                    }
                }
                // Stop processing any input with this key, as we have handled it
                GetTree().SetInputAsHandled();
            }
        }
    }

    private void TextEditorAction_DeleteText()
    {
        Custom_TextEdit_RichTextLabel customLabel = FileLabels[CursorLine];

        // Delete normal
        // ==========
        if (CursorPosition >= customLabel.LineText.Length-1 && customLabel.LineText.Length > 0)
        {
            String lineText = customLabel.LineText;;
            lineText = lineText.Remove(CursorPosition-1, 1);
            customLabel.SetLineText(lineText);
            CursorPosition -= 1;
        }
        else if (CursorPosition - 1 >= 0)
        {
            String lineText = customLabel.LineText;;
            lineText = lineText.Remove(CursorPosition-1, 1);
            customLabel.SetLineText(lineText);
            CursorPosition -= 1;
        }
        else
        {
            // Delete the line
            if (CursorLine > 0)
            {
                int priorLineLength = FileLabels[CursorLine-1].LineText.Length;

                FileLabels[CursorLine-1].SetLineText(FileLabels[CursorLine-1].LineText + FileLabels[CursorLine].LineText);
                
                FileLabels[CursorLine].QueueFree();
                FileLabels.RemoveAt(CursorLine);
                
                CursorPosition = priorLineLength;
                CursorLine -= 1;

                UpdateLineNumbers();
            }
        }
        // ==========
        UpdateSelectedTextLabel();

        // Normal cursor move - so undo any selection
        VirtualCursorSelectionLine = CursorLine;
        VirtualCursorSelectionPosition = CursorPosition;
        UpdateSelectionBasedOnVirtualCursor();
    }
    private void TextEditorAction_DeleteText_Selection()
    {
        Custom_TextEdit_RichTextLabel customLabel = FileLabels[CursorLine];

        // Is it on the same line? If so, then delete the text between positions
        if (CursorSelectionLineStart == CursorSelectionLineEnd)
        {
            Custom_TextEdit_RichTextLabel selectedLabel = FileLabels[CursorSelectionLineStart];
            String selectedLabelText = selectedLabel.LineText;
            String newLabelText = "";
            for (int i = 0; i < selectedLabelText.Length; i++)
            {
                if (i >= CursorSelectionPositionStart && i < CursorSelectionPositionEnd)
                {
                    // Skip!
                }
                else
                {
                    newLabelText += selectedLabelText[i];
                }
            }
            customLabel.SetLineText(newLabelText);
        }
        else
        {
            // Delete text from the start
            Custom_TextEdit_RichTextLabel startLabel = FileLabels[CursorSelectionLineStart];
            String startText = startLabel.LineText;
            String newStartText = "";
            for (int i = 0; i < CursorSelectionPositionStart; i++)
            {
                newStartText += startText[i];
            }
            startLabel.SetLineText(newStartText);

            // Delete text from the end
            Custom_TextEdit_RichTextLabel endLabel = FileLabels[CursorSelectionLineEnd];
            String endText = endLabel.LineText;
            String newEndText = "";
            for (int i = endText.Length; i > CursorSelectionPositionEnd; i--)
            {
                newEndText += endText[i];
            }
            endLabel.SetLineText(newEndText);

            // Delete text from the middle
            List<Custom_TextEdit_RichTextLabel> newFileLabels = new List<Custom_TextEdit_RichTextLabel>();
            for (int i = 0; i < FileLabels.Count; i++)
            {
                if (i > CursorSelectionLineStart && i <= CursorSelectionLineEnd)
                {
                    // Skip adding it, but delete the label
                    FileLabels[i].QueueFree();
                }
                else
                {
                    newFileLabels.Add(FileLabels[i]);
                }
            }
            FileLabels = newFileLabels;

            // Add the text from the end to the start
            startLabel.SetLineText(startLabel.LineText + endLabel.LineText);
        }

        UpdateLineNumbers();
        // Update selection
        CursorLine = CursorSelectionLineStart;
        CursorPosition = CursorSelectionPositionStart;
        UpdateSelectedTextLabel();
        // Undo the selection
        VirtualCursorSelectionLine = CursorLine;
        VirtualCursorSelectionPosition = CursorPosition;
        UpdateSelectionBasedOnVirtualCursor();
    }

    private void TextEditorAction_AddTab()
    {
        Custom_TextEdit_RichTextLabel customLabel = FileLabels[CursorLine];
        String lineText = customLabel.RichLabel.Text;

        // Use insert if there is text, otherwise just append it
        if (CursorPosition < lineText.Length)
        {
            lineText = lineText.Insert(CursorPosition, "\t");
        }
        else
        {
            lineText += '\t';
        }
        customLabel.SetLineText(lineText);
        CursorPosition += 1;
        UpdateSelectedTextLabel();

        // Normal cursor move - so undo any selection
        VirtualCursorSelectionLine = CursorLine;
        VirtualCursorSelectionPosition = CursorPosition;
        UpdateSelectionBasedOnVirtualCursor();
    }
    private void TextEditorAction_PasteClipboard()
    {
        Custom_TextEdit_RichTextLabel customLabel = FileLabels[CursorLine];
        String lineText = customLabel.RichLabel.Text;

        // Are there new lines?
        String clipboardText = OS.Clipboard;
        if (clipboardText.Contains("\n") == true)
        {
            // Remove replace (/r) as we do not want them
            clipboardText = clipboardText.Replace('\r', ' ');

            String[] textLines = clipboardText.Split('\n');
            // Append the first bit of text
            if (CursorPosition < lineText.Length)
            {
                lineText = lineText.Insert(CursorPosition, textLines[0]);
            }
            else
            {
                lineText += textLines[0];
            }
            customLabel.SetLineText(lineText);
            // For the rest, make new lines
            for (int i = 1; i < textLines.Length; i++)
            {
                InsertLine(CursorLine+i, textLines[i]);
            }
            UpdateLineNumbers();
            CursorLine = CursorLine + textLines.Length-1;
            CursorPosition = textLines[textLines.Length-1].Length;
            UpdateSelectedTextLabel();
        }
        else
        {
            // If there are no new lines, just append/insert the text like normal.
            if (CursorPosition < lineText.Length)
            {
                lineText = lineText.Insert(CursorPosition, clipboardText);
            }
            else
            {
                lineText += clipboardText;
            }
            customLabel.SetLineText(lineText);
            CursorPosition += clipboardText.Length;
            UpdateSelectedTextLabel();
        }

        // Normal cursor move - so undo any selection
        VirtualCursorSelectionLine = CursorLine;
        VirtualCursorSelectionPosition = CursorPosition;
        UpdateSelectionBasedOnVirtualCursor();
    }
    private void TextEditorAction_CopyClipboard()
    {
        String clipboardText = "";

        // Is it on the same line?
        if (CursorSelectionLineStart == CursorSelectionLineEnd)
        {
            Custom_TextEdit_RichTextLabel selectedLabel = FileLabels[CursorSelectionLineStart];
            String selectedLabelText = selectedLabel.LineText;
            for (int i = 0; i < selectedLabelText.Length; i++)
            {
                if (i < CursorSelectionPositionStart || i >= CursorSelectionPositionEnd)
                {
                    // Skip!
                }
                else
                {
                    clipboardText += selectedLabelText[i];
                }
            }
        }
        else
        {
            // Add beginning text
            Custom_TextEdit_RichTextLabel startLabel = FileLabels[CursorSelectionLineStart];
            String startText = startLabel.LineText;
            for (int i = CursorSelectionPositionStart; i < startText.Length; i++)
            {
                clipboardText += startText[i];
            }
            clipboardText += '\n';

            // Add the middle text
            for (int i = 0; i < FileLabels.Count; i++)
            {
                if (i > CursorSelectionLineStart && i < CursorSelectionLineEnd)
                {
                    clipboardText += FileLabels[i].LineText;
                    clipboardText += "\n";
                }
            }

            // Add the end text
            Custom_TextEdit_RichTextLabel endLabel = FileLabels[CursorSelectionLineEnd];
            String endText = endLabel.LineText;
            for (int i = 0; i < CursorSelectionPositionEnd; i++)
            {
                clipboardText += endText[i];
            }

            OS.Clipboard = clipboardText;
        }

        OS.Clipboard = clipboardText;
    }
    private void TextEditorAction_AddNewLine()
    {
        // Get the text and split it into two strings
        Custom_TextEdit_RichTextLabel customLabel = FileLabels[CursorLine];
        String labelText = customLabel.RichLabel.Text;
        String labelTextOne = "";
        for (int i = 0; i < CursorPosition; i++)
        {
            labelTextOne += labelText[i];
        }
        String labelTextTwo = "";
        for (int i = CursorPosition; i < labelText.Length; i++)
        {
            labelTextTwo += labelText[i];
        }
        customLabel.SetLineText(labelTextTwo);

        // make a new line
        InsertLine(CursorLine, labelTextOne);

        UpdateLineNumbers();

        CursorLine += 1;
        CursorPosition = 0;
        UpdateSelectedTextLabel();

        // Normal cursor move - so undo any selection
        VirtualCursorSelectionLine = CursorLine;
        VirtualCursorSelectionPosition = CursorPosition;
        UpdateSelectionBasedOnVirtualCursor();
    }
    private void TextEditorAction_NavigateArrowKeys(InputEventKey keyPress)
    {
        bool lineChanged = false;
        if (keyPress.Scancode == (int)KeyList.Right)
        {
            CursorPosition += 1;
        }
        else if (keyPress.Scancode == (int)KeyList.Left)
        {
            CursorPosition -= 1;
        }
        else if (keyPress.Scancode == (int)KeyList.Up)
        {
            CursorLine -= 1;
            lineChanged = true;
        }
        else if (keyPress.Scancode == (int)KeyList.Down)
        {
            CursorLine += 1;
            lineChanged = true;
        }

        // Keep in bounds - lines (pre position check)
        CursorLine = Mathf.Clamp(CursorLine, 0, FileLabels.Count-1);
        // If the line changed, clamp the position to the closest on the next line
        if (lineChanged == true)
        {
            CursorPosition = Mathf.Clamp(CursorPosition, 0, FileLabels[CursorLine].LineText.Length);
        }
        // Keep in bounds - position
        if (CursorPosition < 0)
        {
            if (CursorLine > 0)
            {
                CursorLine -= 1;
                lineChanged = true;
                CursorPosition = 99999; // set it really far so it wraps
            }
            else
            {
                CursorPosition = 0;
            }
        }
        else if (CursorPosition > FileLabels[CursorLine].RichLabel.Text.Length)
        {
            // only move lines if we are not at the end
            if (CursorLine < FileLabels.Count-1)
            {
                CursorLine += 1;
                CursorPosition = 0;
            }
            else
            {
                CursorPosition -= 1;
            }
        }
        // Keep in bounds - lines
        CursorLine = Mathf.Clamp(CursorLine, 0, FileLabels.Count-1);
        // If the line changed, clamp the position to the closest on the next line
        if (lineChanged == true)
        {
            CursorPosition = Mathf.Clamp(CursorPosition, 0, FileLabels[CursorLine].LineText.Length);
        }

        UpdateSelectedTextLabel();
    }
    private void TextEditorAction_NavigateArrowKeys_Selection(InputEventKey keyPress)
    {
        bool lineChanged = false;
        if (keyPress.Scancode == (int)KeyList.Right)
        {
            VirtualCursorSelectionPosition += 1;
        }
        else if (keyPress.Scancode == (int)KeyList.Left)
        {
            VirtualCursorSelectionPosition -= 1;
        }
        else if (keyPress.Scancode == (int)KeyList.Up)
        {
            VirtualCursorSelectionLine -= 1;
            lineChanged = true;
        }
        else if (keyPress.Scancode == (int)KeyList.Down)
        {
            VirtualCursorSelectionLine += 1;
            lineChanged = true;
        }

        // Keep in bounds - lines (pre position check)
        VirtualCursorSelectionLine = Mathf.Clamp(VirtualCursorSelectionLine, 0, FileLabels.Count-1);
        // If the line changed, clamp the position to the closest on the next line
        if (lineChanged == true)
        {
            VirtualCursorSelectionPosition = Mathf.Clamp(VirtualCursorSelectionPosition, 0, FileLabels[VirtualCursorSelectionLine].LineText.Length);
        }
        // Keep in bounds - position
        if (VirtualCursorSelectionPosition < 0)
        {
            if (VirtualCursorSelectionLine > 0)
            {
                VirtualCursorSelectionLine -= 1;
                lineChanged = true;
                VirtualCursorSelectionPosition = 99999; // set it really far so it wraps
            }
            else
            {
                VirtualCursorSelectionPosition = 0;
            }
        }
        else if (VirtualCursorSelectionPosition > FileLabels[VirtualCursorSelectionLine].RichLabel.Text.Length)
        {
            // only move lines if we are not at the end
            if (VirtualCursorSelectionLine < FileLabels.Count-1)
            {
                VirtualCursorSelectionLine += 1;
                VirtualCursorSelectionPosition = 0;
            }
            else
            {
                VirtualCursorSelectionPosition -= 1;
            }
        }
        // Keep in bounds - lines
        VirtualCursorSelectionLine = Mathf.Clamp(VirtualCursorSelectionLine, 0, FileLabels.Count-1);
        // If the line changed, clamp the position to the closest on the next line
        if (lineChanged == true)
        {
            VirtualCursorSelectionPosition = Mathf.Clamp(VirtualCursorSelectionPosition, 0, FileLabels[VirtualCursorSelectionLine].LineText.Length);
        }

        UpdateSelectionBasedOnVirtualCursor();
    }
    public void TextEditorAction_InsertCharacter(InputEventKey keyPress)
    {
        Custom_TextEdit_RichTextLabel customLabel = FileLabels[CursorLine];
        String lineText = customLabel.RichLabel.Text;

        // Only insert if it's a valid character that can be inputted
        // (I.E - not a control character)
        char keyAsCode = (char)keyPress.Unicode;
        if (Char.IsControl(keyAsCode) == false)
        {
            // Use insert if there is text, otherwise just append it
            if (CursorPosition < lineText.Length)
            {
                lineText = lineText.Insert(CursorPosition, "" + keyAsCode);
            }
            else
            {
                lineText += keyAsCode;
            }
            
            customLabel.SetLineText(lineText);
            CursorPosition += 1;
            UpdateSelectedTextLabel();

            VirtualCursorSelectionLine = CursorLine;
            VirtualCursorSelectionPosition = CursorPosition;
            UpdateSelectionBasedOnVirtualCursor();
        }
    }
    public int GetCharacterAtPosition(Vector2 inputPosition)
    {
        // Was it on a RichTextLabel?
        Custom_TextEdit_RichTextLabel selectedLabel = null;
        for (int i = 0; i < FileLabels.Count; i++)
        {
            if (FileLabels[i].GetGlobalRect().HasPoint(inputPosition) == true)
            {
                selectedLabel = FileLabels[i];
                break;
            }
        }

        if (selectedLabel != null)
        {
            // Find the selected character
            Vector2 textSize = EditorFont.GetWordwrapStringSize(selectedLabel.RichLabel.Text, selectedLabel.RectSize.x);
            Vector2 localPoint = selectedLabel.RichLabel.GetGlobalTransform().Inverse() * inputPosition;

            if (localPoint.x > textSize.x || localPoint.y > textSize.y)
            {
                return -1;
            }
            else
            {
                Vector2 currentProcessSize = Vector2.Zero;
                Vector2 currentCharSize = Vector2.Zero;
                for (int i = 0; i < selectedLabel.RichLabel.Text.Length; i++)
                {
                    currentCharSize = EditorFont.GetCharSize(selectedLabel.RichLabel.Text[i]);
                    currentProcessSize.x += currentCharSize.x;
                    if (currentProcessSize.x >= textSize.x)
                    {
                        currentProcessSize.x = 0;
                        currentProcessSize.y += currentCharSize.y;
                    }

                    // is the mouse inside size?
                    if (localPoint.x <= currentProcessSize.x && localPoint.y <= currentProcessSize.y + currentCharSize.y)
                    {
                        return (selectedLabel.RichLabel.Text[i]);
                    }
                }
                return -1;
            }
        }
        else
        {
            return -1;
        }
    }



    public void SetCursorToPosition(Vector2 inputPosition, bool setSelectionCursor=false)
    {
        // Was it on a RichTextLabel?
        Custom_TextEdit_RichTextLabel selectedLabel = null;
        int selectedLabelNum = 0;
        for (int i = 0; i < FileLabels.Count; i++)
        {
            if (FileLabels[i].RichLabel.GetGlobalRect().HasPoint(inputPosition) == true)
            {
                selectedLabel = FileLabels[i];
                selectedLabelNum = i;
                break;
            }
        }

        if (selectedLabel != null)
        {
            if (setSelectionCursor == false)
            {
                CursorLine = selectedLabelNum;
            }
            else
            {
                VirtualCursorSelectionLine = selectedLabelNum;
            }

            // Find the selected character
            Vector2 textSize = selectedLabel.EditorFont.GetStringSize(selectedLabel.LineText);
            Vector2 localPoint = selectedLabel.RichLabel.GetGlobalTransform().Inverse() * inputPosition;
            // Add a *slight* offset so its easier to get characters
            localPoint.x += 0.5f;
            
            if (localPoint.x > textSize.x || localPoint.y > textSize.y)
            {
                if (setSelectionCursor == false)
                {
                    CursorPosition = selectedLabel.RichLabel.Text.Length;
                }
                else
                {
                    VirtualCursorSelectionPosition = selectedLabel.RichLabel.Text.Length;
                }
            }
            else
            {
                bool didSetCursorPosition = false;
                Vector2 currentProcessSize = Vector2.Zero;
                Vector2 currentCharSize = Vector2.Zero;
                for (int i = 0; i < selectedLabel.RichLabel.Text.Length; i++)
                {
                    currentCharSize = EditorFont.GetCharSize(selectedLabel.RichLabel.Text[i]);
                    currentProcessSize.x += currentCharSize.x;
                    if (currentProcessSize.x >= textSize.x)
                    {
                        currentProcessSize.x = 0;
                        currentProcessSize.y += currentCharSize.y;
                    }

                    // is the mouse inside size?
                    if (localPoint.x <= currentProcessSize.x && localPoint.y <= currentProcessSize.y + currentCharSize.y)
                    {
                        didSetCursorPosition = true;

                        if (setSelectionCursor == false)
                        {
                            CursorPosition = i;
                        }
                        else
                        {
                            VirtualCursorSelectionPosition = i;
                        }
                        
                        break;
                    }
                }

                if (didSetCursorPosition == false)
                {
                    if (setSelectionCursor == false)
                    {
                        CursorPosition = selectedLabel.RichLabel.Text.Length;
                    }
                    else
                    {
                        VirtualCursorSelectionPosition = selectedLabel.RichLabel.Text.Length;
                    }
                }
            }
        }
        else
        {
            if (setSelectionCursor == false)
            {
                CursorLine = 0;
                CursorPosition = 0;
            }
            else
            {
                VirtualCursorSelectionLine = 0;
                VirtualCursorSelectionPosition = 0;
            }
        }

        if (setSelectionCursor == false)
        {
            UpdateSelectedTextLabel();
        }
        else
        {
            UpdateSelectionBasedOnVirtualCursor();
        }
    }

    private void UpdateSelectedTextLabel()
    {
        if (LastSelectedLabel != null)
        {
            LastSelectedLabel.UpdateSelected(false, 0);
            LastSelectedLabel = null;
        }
        if (CursorLine < FileLabels.Count)
        {
            LastSelectedLabel = FileLabels[CursorLine];
            LastSelectedLabel.UpdateSelected(true, CursorPosition);
        }
        else
        {
            LastSelectedLabel = null;
        }
    }


    private void UpdateSelectionBasedOnVirtualCursor()
    {
        if (CursorLine < VirtualCursorSelectionLine)
        {
            CursorSelectionLineStart = CursorLine;
            CursorSelectionPositionStart = CursorPosition;
            
            CursorSelectionLineEnd = VirtualCursorSelectionLine;
            CursorSelectionPositionEnd = VirtualCursorSelectionPosition;
        }
        else if (CursorLine > VirtualCursorSelectionLine)
        {
            CursorSelectionLineStart = VirtualCursorSelectionLine;
            CursorSelectionPositionStart = VirtualCursorSelectionPosition;

            CursorSelectionLineEnd = CursorLine;
            CursorSelectionPositionEnd = CursorPosition;
        }
        else
        {
            CursorSelectionLineStart = CursorLine;
            CursorSelectionLineEnd = CursorLine;

            CursorSelectionPositionStart = Mathf.Min(CursorPosition, VirtualCursorSelectionPosition);
            CursorSelectionPositionEnd = Mathf.Max(CursorPosition, VirtualCursorSelectionPosition);
        }

        // TODO - optimize this!
        // Until then, just draw the changes dumbly
        for (int i = 0; i < FileLabels.Count; i++)
        {
            FileLabels[i].Update();
        }
    }
}
