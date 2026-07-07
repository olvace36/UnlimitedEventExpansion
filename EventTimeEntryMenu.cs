using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using UnlimitedEventExpansion;
using StardewModdingAPI;
using System.Reflection;

namespace UnlimitedEventExpansion
{
    public partial class ModEntry
    {
        private const string TooLateToScheduleMessage = "Too late to schedule this event today.";
        private const string TimeSlotUnavailableMessage = "Cannot schedule at this time";

        private sealed class EventTimeEntryMenu : IClickableMenu
        {
            private static readonly string[] ControllerStyleOptionNames = { "gamepadControls", "GamepadControls" };
            private static readonly string[] SnappyMenuOptionNames = { "snappyMenus", "SnappyMenus" };

            private readonly Action<string> onConfirm;
            private readonly Action onCancel;
            private readonly string npcDisplayName;
            private readonly string eventDisplayName;

            private readonly TextBox timeTextBox;
            private readonly Rectangle textBoxBounds;
            private readonly Rectangle minusButtonBounds;
            private readonly Rectangle plusButtonBounds;
            private readonly Rectangle primaryActionButtonBounds;
            private readonly Rectangle cancelButtonBounds;
            private readonly string primaryActionLabel;

            private string validationMessage = string.Empty;
            private bool controllerCursorOverrideApplied;
            private bool? previousControllerStyleMenus;
            private bool? previousSnappyMenus;

            public EventTimeEntryMenu(
                string npcDisplayName,
                string eventDisplayName,
                Action<string> onConfirm,
                Action onCancel,
                string? initialTimeText = null,
                string? primaryActionLabel = null)
                : base(Game1.uiViewport.Width / 2 - 320, Game1.uiViewport.Height / 2 - 210, 640, 420, showUpperRightCloseButton: false)
            {
                this.onConfirm = onConfirm ?? (_ => { });
                this.onCancel = onCancel ?? (() => { });
                this.npcDisplayName = string.IsNullOrWhiteSpace(npcDisplayName) ? "NPC" : npcDisplayName;
                this.eventDisplayName = string.IsNullOrWhiteSpace(eventDisplayName) ? "event" : eventDisplayName;
                this.primaryActionLabel = string.IsNullOrWhiteSpace(primaryActionLabel) ? "Confirm" : primaryActionLabel;

                textBoxBounds = new Rectangle(xPositionOnScreen + 170, yPositionOnScreen + 198, 220, 56);
                minusButtonBounds = new Rectangle(xPositionOnScreen + 106, yPositionOnScreen + 202, 52, 52);
                plusButtonBounds = new Rectangle(xPositionOnScreen + 402, yPositionOnScreen + 202, 52, 52);
                cancelButtonBounds = new Rectangle(xPositionOnScreen + 58, yPositionOnScreen + height - 88, 130, 56);
                primaryActionButtonBounds = new Rectangle(xPositionOnScreen + width - 188, yPositionOnScreen + height - 88, 130, 56);

                timeTextBox = new TextBox(
                    Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
                    null,
                    Game1.smallFont,
                    Game1.textColor)
                {
                    X = textBoxBounds.X,
                    Y = textBoxBounds.Y,
                    Width = textBoxBounds.Width,
                    Height = textBoxBounds.Height,
                    Text = string.IsNullOrWhiteSpace(initialTimeText) ? BuildDefaultSuggestedTime() : initialTimeText
                };
                timeTextBox.textLimit = 5;
                timeTextBox.Selected = true;
                Game1.keyboardDispatcher.Subscriber = timeTextBox;
                EnableFreeControllerCursorForThisMenu();

                if (!TryGetMinimumAllowedEventTime(out _))
                    validationMessage = TooLateToScheduleMessage;
            }

            protected override void cleanupBeforeExit()
            {
                ReleaseTextInputAndRestoreControllerCursor();
                base.cleanupBeforeExit();
            }

            public override void emergencyShutDown()
            {
                ReleaseTextInputAndRestoreControllerCursor();
                base.emergencyShutDown();
            }

            public override void update(GameTime time)
            {
                base.update(time);
                timeTextBox.Update();
            }

            public override void receiveLeftClick(int x, int y, bool playSound = true)
            {
                if (primaryActionButtonBounds.Contains(x, y))
                {
                    TrySubmit();
                    return;
                }

                if (cancelButtonBounds.Contains(x, y))
                {
                    CancelAndClose();
                    return;
                }

                if (minusButtonBounds.Contains(x, y))
                {
                    AdjustTimeByMinutes(-10);
                    FocusTextBox();
                    return;
                }

                if (plusButtonBounds.Contains(x, y))
                {
                    AdjustTimeByMinutes(10);
                    FocusTextBox();
                    return;
                }

                bool clickedTextBox = textBoxBounds.Contains(x, y);
                timeTextBox.Selected = clickedTextBox;
                if (clickedTextBox)
                {
                    Game1.keyboardDispatcher.Subscriber = timeTextBox;
                }
                else if (Game1.keyboardDispatcher.Subscriber == timeTextBox)
                {
                    Game1.keyboardDispatcher.Subscriber = null;
                }
            }

            public override void receiveKeyPress(Keys key)
            {
                if (key == Keys.Enter)
                {
                    TrySubmit();
                    return;
                }

                if (key == Keys.Escape)
                {
                    CancelAndClose();
                    return;
                }

                if (key == Keys.OemPlus || key == Keys.Add)
                {
                    AdjustTimeByMinutes(10);
                    return;
                }

                if (key == Keys.OemMinus || key == Keys.Subtract)
                {
                    AdjustTimeByMinutes(-10);
                    return;
                }

                base.receiveKeyPress(key);
            }

            public override void receiveRightClick(int x, int y, bool playSound = true)
            {
                CancelAndClose();
            }

            public override void draw(SpriteBatch b)
            {
                Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);

                string title = string.IsNullOrWhiteSpace(npcDisplayName)
                    ? $"Schedule {eventDisplayName}"
                    : $"Schedule {eventDisplayName} with {npcDisplayName}";
                Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
                Vector2 titlePosition = new Vector2(xPositionOnScreen + (width - titleSize.X) / 2f, yPositionOnScreen + 96f);
                Utility.drawTextWithShadow(b, title, Game1.dialogueFont, titlePosition, Game1.textColor);
                TryGetMinimumAllowedEventTime(out string minimumAllowedPreview);
                Utility.drawTextWithShadow(
                    b,
                    $"Enter HHMM ({FormatEventTimeForDisplay(minimumAllowedPreview)} - 23:00)",
                    Game1.smallFont,
                    new Vector2(xPositionOnScreen + 58, yPositionOnScreen + 152),
                    Game1.textColor);

                DrawButton(b, minusButtonBounds, "-", new Color(235, 235, 235));
                DrawButton(b, plusButtonBounds, "+", new Color(235, 235, 235));

                IClickableMenu.drawTextureBox(
                    b,
                    Game1.menuTexture,
                    new Rectangle(0, 256, 60, 60),
                    textBoxBounds.X,
                    textBoxBounds.Y - 6,
                    textBoxBounds.Width + 16,
                    textBoxBounds.Height + 12,
                    Color.White,
                    1f,
                    false);
                timeTextBox.Draw(b);

                string digitsOnly = new string((timeTextBox.Text ?? string.Empty).Where(char.IsDigit).ToArray());
                if (TryValidateEventTimeSelection(digitsOnly, out string normalizedPreviewTime, out string previewValidationError))
                {
                    Utility.drawTextWithShadow(
                        b,
                        $"Selected: {FormatEventTimeForDisplay(normalizedPreviewTime)}",
                        Game1.smallFont,
                        new Vector2(xPositionOnScreen + 58, yPositionOnScreen + 278),
                        Game1.textColor);
                }
                else
                {


                    if (previewValidationError == TimeSlotUnavailableMessage)
                    {
                        Utility.drawTextWithShadow(
                            b,
                            TimeSlotUnavailableMessage,
                            Game1.smallFont,
                            new Vector2(xPositionOnScreen + 58, yPositionOnScreen + 278),
                            Color.IndianRed);
                    }
                    else
                    {
                        Utility.drawTextWithShadow(
                        b,
                        "Selected: Invalid time",
                        Game1.smallFont,
                        new Vector2(xPositionOnScreen + 58, yPositionOnScreen + 278),
                        Color.IndianRed);
                    }
                }

                if (string.IsNullOrEmpty(minimumAllowedPreview))
                {
                    Utility.drawTextWithShadow(
                        b,
                        TooLateToScheduleMessage,
                        Game1.smallFont,
                        new Vector2(xPositionOnScreen + 58, yPositionOnScreen + 298),
                        Color.IndianRed);
                }

                DrawButton(b, primaryActionButtonBounds, primaryActionLabel, new Color(196, 236, 196));
                DrawButton(b, cancelButtonBounds, "Cancel", new Color(242, 218, 218));

                if (!string.IsNullOrWhiteSpace(validationMessage))
                {
                    Utility.drawTextWithShadow(
                        b,
                        validationMessage,
                        Game1.smallFont,
                        new Vector2(xPositionOnScreen + 58, yPositionOnScreen + height - 112),
                        Color.IndianRed);
                }

                drawMouse(b);
            }

            private static void DrawButton(SpriteBatch b, Rectangle bounds, string text, Color boxColor)
            {
                IClickableMenu.drawTextureBox(
                    b,
                    Game1.menuTexture,
                    new Rectangle(0, 256, 60, 60),
                    bounds.X,
                    bounds.Y,
                    bounds.Width,
                    bounds.Height,
                    boxColor,
                    1f,
                    false);

                Vector2 textSize = Game1.smallFont.MeasureString(text);
                Vector2 textPosition = new Vector2(
                    bounds.X + (bounds.Width - textSize.X) / 2f,
                    bounds.Y + (bounds.Height - textSize.Y) / 2f + 2f);

                Utility.drawTextWithShadow(b, text, Game1.smallFont, textPosition, Game1.textColor);
            }

            private static string BuildDefaultSuggestedTime()
            {
                if (TryGetMinimumAllowedEventTime(out string minimumAllowedTime))
                    return minimumAllowedTime;

                return "2300";
            }

            private void FocusTextBox()
            {
                timeTextBox.Selected = true;
                Game1.keyboardDispatcher.Subscriber = timeTextBox;
            }

            private void CancelAndClose()
            {
                ReleaseTextInputAndRestoreControllerCursor();
                Game1.playSound("bigDeSelect");
                onCancel();
            }

            private void TrySubmit()
            {
                validationMessage = string.Empty;
                if (!TryValidateEventTimeSelection(timeTextBox.Text, out string normalizedEventTime, out string validationError))
                {
                    validationMessage = validationError;
                    Game1.playSound("cancel");
                    return;
                }

                ReleaseTextInputAndRestoreControllerCursor();
                Game1.playSound("smallSelect");
                onConfirm(normalizedEventTime);
            }

            private void AdjustTimeByMinutes(int deltaMinutes)
            {
                if (!TryGetAllowedEventTimeWindow(out int minimumAllowedMinutes, out int latestAllowedMinutes))
                {
                    validationMessage = TooLateToScheduleMessage;
                    Game1.playSound("cancel");
                    return;
                }

                string digitsOnly = new string((timeTextBox.Text ?? string.Empty).Where(char.IsDigit).ToArray());
                if (!TryNormalizeEventTime(digitsOnly, out string normalizedCurrentTime))
                    normalizedCurrentTime = BuildDefaultSuggestedTime();

                if (!int.TryParse(normalizedCurrentTime, out int currentTime))
                    currentTime = ((minimumAllowedMinutes / 60) * 100) + (minimumAllowedMinutes % 60);

                int currentTotalMinutes = ((currentTime / 100) * 60) + (currentTime % 100);
                currentTotalMinutes = Math.Clamp(currentTotalMinutes, minimumAllowedMinutes, latestAllowedMinutes);

                int adjustedTotalMinutes = Math.Clamp(currentTotalMinutes + deltaMinutes, minimumAllowedMinutes, latestAllowedMinutes);
                adjustedTotalMinutes = (adjustedTotalMinutes / 10) * 10;
                if (adjustedTotalMinutes < minimumAllowedMinutes)
                    adjustedTotalMinutes = minimumAllowedMinutes;

                int adjustedTime = ((adjustedTotalMinutes / 60) * 100) + (adjustedTotalMinutes % 60);
                timeTextBox.Text = $"{adjustedTime:0000}";
                validationMessage = string.Empty;
                Game1.playSound("smallSelect");
            }

            private void EnableFreeControllerCursorForThisMenu()
            {
                object? options = Game1.options;
                if (options is null)
                    return;

                if (TryReadBooleanOption(options, ControllerStyleOptionNames, out bool controllerStyleEnabled))
                {
                    previousControllerStyleMenus = controllerStyleEnabled;
                    TryWriteBooleanOption(options, ControllerStyleOptionNames, false);
                    controllerCursorOverrideApplied = true;
                }

                if (TryReadBooleanOption(options, SnappyMenuOptionNames, out bool snappyMenusEnabled))
                {
                    previousSnappyMenus = snappyMenusEnabled;
                    TryWriteBooleanOption(options, SnappyMenuOptionNames, false);
                    controllerCursorOverrideApplied = true;
                }
            }

            private void ReleaseTextInputAndRestoreControllerCursor()
            {
                if (Game1.keyboardDispatcher.Subscriber == timeTextBox)
                    Game1.keyboardDispatcher.Subscriber = null;

                timeTextBox.Selected = false;
                RestoreControllerCursorOptions();
            }

            private void RestoreControllerCursorOptions()
            {
                if (!controllerCursorOverrideApplied)
                    return;

                object? options = Game1.options;
                if (options is null)
                    return;

                if (previousControllerStyleMenus.HasValue)
                    TryWriteBooleanOption(options, ControllerStyleOptionNames, previousControllerStyleMenus.Value);

                if (previousSnappyMenus.HasValue)
                    TryWriteBooleanOption(options, SnappyMenuOptionNames, previousSnappyMenus.Value);

                controllerCursorOverrideApplied = false;
            }

            private static bool TryReadBooleanOption(object options, string[] memberNames, out bool value)
            {
                foreach (string memberName in memberNames)
                {
                    if (TryReadBooleanOption(options, memberName, out value))
                        return true;
                }

                value = default;
                return false;
            }

            private static bool TryReadBooleanOption(object options, string memberName, out bool value)
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                Type optionsType = options.GetType();

                PropertyInfo? property = optionsType.GetProperty(memberName, flags);
                if (property?.CanRead == true && property.PropertyType == typeof(bool) && property.GetValue(options) is bool propertyValue)
                {
                    value = propertyValue;
                    return true;
                }

                FieldInfo? field = optionsType.GetField(memberName, flags);
                if (field?.FieldType == typeof(bool) && field.GetValue(options) is bool fieldValue)
                {
                    value = fieldValue;
                    return true;
                }

                value = default;
                return false;
            }

            private static bool TryWriteBooleanOption(object options, string[] memberNames, bool value)
            {
                foreach (string memberName in memberNames)
                {
                    if (TryWriteBooleanOption(options, memberName, value))
                        return true;
                }

                return false;
            }

            private static bool TryWriteBooleanOption(object options, string memberName, bool value)
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                Type optionsType = options.GetType();

                PropertyInfo? property = optionsType.GetProperty(memberName, flags);
                if (property?.CanWrite == true && property.PropertyType == typeof(bool))
                {
                    property.SetValue(options, value);
                    return true;
                }

                FieldInfo? field = optionsType.GetField(memberName, flags);
                if (field?.FieldType == typeof(bool) && !field.IsInitOnly)
                {
                    field.SetValue(options, value);
                    return true;
                }

                return false;
            }

        }

        private sealed class EventLocationOption
        {
            public string Key { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public int MaxParticipants { get; set; }
            public List<string> RequiredNpcNames { get; set; } = new();
        }

        private sealed class EventNpcSelectionInfo
        {
            public List<string> AllNames { get; set; } = new();
            public HashSet<string> LockedNames { get; } = new(StringComparer.OrdinalIgnoreCase);
            public int OptionalLimit { get; set; }
        }

        private sealed class EventLocationSelectionMenu : IClickableMenu
        {
            private readonly Action<string> onNext;
            private readonly Action onBack;
            private readonly Action onCancel;
            private readonly string npcDisplayName;
            private readonly string eventDisplayName;
            private readonly List<EventLocationOption> options;

            private readonly Rectangle listBounds;
            private readonly Rectangle backButtonBounds;
            private readonly Rectangle nextButtonBounds;
            private readonly Rectangle cancelButtonBounds;
            private readonly Rectangle upButtonBounds;
            private readonly Rectangle downButtonBounds;

            private readonly int rowHeight = 56;
            private readonly int visibleRows = 6;

            private int selectedIndex;
            private int scrollIndex;
            private string validationMessage = string.Empty;

            public EventLocationSelectionMenu(
                string npcDisplayName,
                string eventDisplayName,
                List<EventLocationOption> options,
                string? selectedLocationKey,
                Action<string> onNext,
                Action onBack,
                Action onCancel)
                : base(Game1.uiViewport.Width / 2 - 380, Game1.uiViewport.Height / 2 - 290, 760, 580, showUpperRightCloseButton: false)
            {
                this.npcDisplayName = string.IsNullOrWhiteSpace(npcDisplayName) ? "NPC" : npcDisplayName;
                this.eventDisplayName = string.IsNullOrWhiteSpace(eventDisplayName) ? "event" : eventDisplayName;
                this.options = options ?? new List<EventLocationOption>();
                this.onNext = onNext ?? (_ => { });
                this.onBack = onBack ?? (() => { });
                this.onCancel = onCancel ?? (() => { });

                listBounds = new Rectangle(xPositionOnScreen + 58, yPositionOnScreen + 180, width - 150, 360);
                cancelButtonBounds = new Rectangle(xPositionOnScreen + 58, yPositionOnScreen + height - 82 + 60, 130, 52);
                backButtonBounds = new Rectangle(xPositionOnScreen + width - 328, yPositionOnScreen + height - 82 + 60, 130, 52);
                nextButtonBounds = new Rectangle(xPositionOnScreen + width - 178, yPositionOnScreen + height - 82 + 60, 130, 52);
                upButtonBounds = new Rectangle(listBounds.Right + 10, listBounds.Y, 30, 30);
                downButtonBounds = new Rectangle(listBounds.Right + 10, listBounds.Bottom - 30, 30, 30);

                selectedIndex = -1;
                if (!string.IsNullOrWhiteSpace(selectedLocationKey))
                {
                    int existingIndex = this.options.FindIndex(option => string.Equals(option.Key, selectedLocationKey, StringComparison.OrdinalIgnoreCase));
                    if (existingIndex >= 0)
                        selectedIndex = existingIndex;
                }

                if (selectedIndex >= 0)
                    EnsureSelectionVisible();
            }

            public override void receiveScrollWheelAction(int direction)
            {
                if (direction > 0)
                    scrollIndex = Math.Max(0, scrollIndex - 1);
                else if (direction < 0)
                    scrollIndex = Math.Min(Math.Max(0, options.Count - visibleRows), scrollIndex + 1);
            }

            public override void receiveLeftClick(int x, int y, bool playSound = true)
            {
                if (cancelButtonBounds.Contains(x, y))
                {
                    Game1.playSound("bigDeSelect");
                    onCancel();
                    return;
                }

                if (backButtonBounds.Contains(x, y))
                {
                    Game1.playSound("smallSelect");
                    onBack();
                    return;
                }

                if (nextButtonBounds.Contains(x, y))
                {
                    TryContinue();
                    return;
                }

                if (upButtonBounds.Contains(x, y))
                {
                    scrollIndex = Math.Max(0, scrollIndex - 1);
                    return;
                }

                if (downButtonBounds.Contains(x, y))
                {
                    scrollIndex = Math.Min(Math.Max(0, options.Count - visibleRows), scrollIndex + 1);
                    return;
                }

                for (int row = 0; row < visibleRows; row++)
                {
                    int optionIndex = scrollIndex + row;
                    if (optionIndex >= options.Count)
                        break;

                    Rectangle rowBounds = GetRowBounds(row);
                    if (!rowBounds.Contains(x, y))
                        continue;

                    selectedIndex = optionIndex;
                    validationMessage = string.Empty;
                    Game1.playSound("smallSelect");
                    return;
                }
            }

            public override void receiveKeyPress(Keys key)
            {
                if (key == Keys.Escape)
                {
                    Game1.playSound("bigDeSelect");
                    onCancel();
                    return;
                }

                if (key == Keys.Enter)
                {
                    TryContinue();
                    return;
                }

                if (key == Keys.Up)
                {
                    selectedIndex = selectedIndex < 0 ? 0 : Math.Max(0, selectedIndex - 1);
                    EnsureSelectionVisible();
                    return;
                }

                if (key == Keys.Down)
                {
                    selectedIndex = selectedIndex < 0 ? 0 : Math.Min(options.Count - 1, selectedIndex + 1);
                    EnsureSelectionVisible();
                    return;
                }

                base.receiveKeyPress(key);
            }

            public override void draw(SpriteBatch b)
            {
                Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);

                string title = string.IsNullOrWhiteSpace(npcDisplayName)
                    ? $"Pick {eventDisplayName} location"
                    : $"Pick {eventDisplayName} location ({npcDisplayName})";
                Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
                Utility.drawTextWithShadow(
                    b,
                    title,
                    Game1.dialogueFont,
                    new Vector2(xPositionOnScreen + (width - titleSize.X) / 2f, yPositionOnScreen + 92f),
                    Game1.textColor);

                Utility.drawTextWithShadow(
                    b,
                    "Select one location.",
                    Game1.smallFont,
                    new Vector2(xPositionOnScreen + 58, yPositionOnScreen + 132),
                    Game1.textColor);

                IClickableMenu.drawTextureBox(
                    b,
                    Game1.menuTexture,
                    new Rectangle(0, 256, 60, 60),
                    listBounds.X,
                    listBounds.Y,
                    listBounds.Width,
                    listBounds.Height,
                    Color.White,
                    1f,
                    false);

                for (int row = 0; row < visibleRows; row++)
                {
                    int optionIndex = scrollIndex + row;
                    if (optionIndex >= options.Count)
                        break;

                    EventLocationOption option = options[optionIndex];
                    Rectangle rowBounds = GetRowBounds(row);
                    bool isSelected = optionIndex == selectedIndex;

                    IClickableMenu.drawTextureBox(
                        b,
                        Game1.menuTexture,
                        new Rectangle(0, 256, 60, 60),
                        rowBounds.X,
                        rowBounds.Y,
                        rowBounds.Width,
                        rowBounds.Height,
                        isSelected ? new Color(213, 236, 255) : new Color(245, 245, 245),
                        1f,
                        false);

                    Utility.drawTextWithShadow(
                        b,
                        option.DisplayName,
                        Game1.smallFont,
                        new Vector2(rowBounds.X + 12, rowBounds.Y + 10),
                        Game1.textColor);
                }

                if (options.Count > visibleRows)
                {
                    DrawSimpleButton(b, upButtonBounds, "^", new Color(232, 232, 232));
                    DrawSimpleButton(b, downButtonBounds, "v", new Color(232, 232, 232));
                }

                DrawSimpleButton(b, cancelButtonBounds, "Cancel", new Color(242, 218, 218));
                DrawSimpleButton(b, backButtonBounds, "Back", new Color(225, 225, 225));
                DrawSimpleButton(b, nextButtonBounds, "Next", new Color(196, 236, 196));

                if (!string.IsNullOrWhiteSpace(validationMessage))
                {
                    Utility.drawTextWithShadow(
                        b,
                        validationMessage,
                        Game1.smallFont,
                        new Vector2(xPositionOnScreen + 58, yPositionOnScreen + height - 108),
                        Color.IndianRed);
                }

                drawMouse(b);
            }

            private Rectangle GetRowBounds(int row)
            {
                return new Rectangle(
                    listBounds.X + 8,
                    listBounds.Y + 8 + (row * rowHeight),
                    listBounds.Width - 16,
                    rowHeight - 4);
            }

            private void EnsureSelectionVisible()
            {
                if (selectedIndex < scrollIndex)
                    scrollIndex = selectedIndex;

                if (selectedIndex >= scrollIndex + visibleRows)
                    scrollIndex = selectedIndex - visibleRows + 1;
            }

            private void TryContinue()
            {
                if (selectedIndex < 0 || selectedIndex >= options.Count)
                {
                    validationMessage = "Select a location before continuing.";
                    Game1.playSound("cancel");
                    return;
                }

                Game1.playSound("smallSelect");
                onNext(options[selectedIndex].Key);
            }
        }

        private sealed class EventNpcSelectionMenu : IClickableMenu
        {
            private readonly Action<List<string>> onConfirm;
            private readonly Action onBack;
            private readonly Action onCancel;
            private readonly string npcDisplayName;
            private readonly string eventDisplayName;

            private readonly List<string> allNames;
            private readonly HashSet<string> lockedNames;
            private readonly HashSet<string> selectedNames;
            private readonly int optionalLimit;

            private readonly Rectangle listBounds;
            private readonly Rectangle confirmButtonBounds;
            private readonly Rectangle backButtonBounds;
            private readonly Rectangle cancelButtonBounds;
            private readonly Rectangle upButtonBounds;
            private readonly Rectangle downButtonBounds;

            private readonly int rowHeight = 56;
            private readonly int visibleRows = 6;
            private int scrollIndex;

            private string validationMessage = string.Empty;

            public EventNpcSelectionMenu(
                string npcDisplayName,
                string eventDisplayName,
                EventNpcSelectionInfo info,
                IEnumerable<string> preselectedNames,
                Action<List<string>> onConfirm,
                Action onBack,
                Action onCancel)
                : base(Game1.uiViewport.Width / 2 - 380, Game1.uiViewport.Height / 2 - 290, 760, 580, showUpperRightCloseButton: false)
            {
                this.npcDisplayName = string.IsNullOrWhiteSpace(npcDisplayName) ? "NPC" : npcDisplayName;
                this.eventDisplayName = string.IsNullOrWhiteSpace(eventDisplayName) ? "event" : eventDisplayName;
                this.onConfirm = onConfirm ?? (_ => { });
                this.onBack = onBack ?? (() => { });
                this.onCancel = onCancel ?? (() => { });

                allNames = info?.AllNames?.ToList() ?? new List<string>();
                lockedNames = info?.LockedNames != null
                    ? new HashSet<string>(info.LockedNames, StringComparer.OrdinalIgnoreCase)
                    : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                selectedNames = new HashSet<string>(lockedNames, StringComparer.OrdinalIgnoreCase);
                if (preselectedNames != null)
                {
                    foreach (string selectedName in preselectedNames)
                    {
                        if (allNames.Any(name => string.Equals(name, selectedName, StringComparison.OrdinalIgnoreCase)))
                            selectedNames.Add(selectedName);
                    }
                }

                optionalLimit = Math.Max(0, info?.OptionalLimit ?? 0);

                listBounds = new Rectangle(xPositionOnScreen + 58, yPositionOnScreen + 180, width - 150, 360);
                cancelButtonBounds = new Rectangle(xPositionOnScreen + 58, yPositionOnScreen + height - 82 + 60, 130, 52);
                backButtonBounds = new Rectangle(xPositionOnScreen + width - 328, yPositionOnScreen + height - 82 + 60, 130, 52);
                confirmButtonBounds = new Rectangle(xPositionOnScreen + width - 178, yPositionOnScreen + height - 82 + 60, 130, 52);
                upButtonBounds = new Rectangle(listBounds.Right + 10, listBounds.Y, 30, 30);
                downButtonBounds = new Rectangle(listBounds.Right + 10, listBounds.Bottom - 30, 30, 30);
            }

            public override void receiveScrollWheelAction(int direction)
            {
                if (direction > 0)
                    scrollIndex = Math.Max(0, scrollIndex - 1);
                else if (direction < 0)
                    scrollIndex = Math.Min(Math.Max(0, allNames.Count - visibleRows), scrollIndex + 1);
            }

            public override void receiveLeftClick(int x, int y, bool playSound = true)
            {
                if (cancelButtonBounds.Contains(x, y))
                {
                    Game1.playSound("bigDeSelect");
                    onCancel();
                    return;
                }

                if (backButtonBounds.Contains(x, y))
                {
                    Game1.playSound("smallSelect");
                    onBack();
                    return;
                }

                if (confirmButtonBounds.Contains(x, y))
                {
                    TryConfirm();
                    return;
                }

                if (upButtonBounds.Contains(x, y))
                {
                    scrollIndex = Math.Max(0, scrollIndex - 1);
                    return;
                }

                if (downButtonBounds.Contains(x, y))
                {
                    scrollIndex = Math.Min(Math.Max(0, allNames.Count - visibleRows), scrollIndex + 1);
                    return;
                }

                for (int row = 0; row < visibleRows; row++)
                {
                    int optionIndex = scrollIndex + row;
                    if (optionIndex >= allNames.Count)
                        break;

                    Rectangle rowBounds = GetRowBounds(row);
                    if (!rowBounds.Contains(x, y))
                        continue;

                    string name = allNames[optionIndex];
                    ToggleSelection(name);
                    return;
                }
            }

            public override void receiveKeyPress(Keys key)
            {
                if (key == Keys.Escape)
                {
                    Game1.playSound("bigDeSelect");
                    onCancel();
                    return;
                }

                if (key == Keys.Enter)
                {
                    TryConfirm();
                    return;
                }

                base.receiveKeyPress(key);
            }

            public override void draw(SpriteBatch b)
            {
                Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);

                string title = string.IsNullOrWhiteSpace(npcDisplayName)
                    ? $"Pick guests for {eventDisplayName}"
                    : $"Pick attendees for {eventDisplayName}";
                Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
                Utility.drawTextWithShadow(
                    b,
                    title,
                    Game1.dialogueFont,
                    new Vector2(xPositionOnScreen + (width - titleSize.X) / 2f, yPositionOnScreen + 92f),
                    Game1.textColor);

                int optionalSelected = selectedNames.Count(name => !lockedNames.Contains(name));
                Utility.drawTextWithShadow(
                    b,
                    $"Locked: main/required NPCs. Optional selected: {optionalSelected}/{optionalLimit}",
                    Game1.smallFont,
                    new Vector2(xPositionOnScreen + 58, yPositionOnScreen + 132),
                    Game1.textColor);

                IClickableMenu.drawTextureBox(
                    b,
                    Game1.menuTexture,
                    new Rectangle(0, 256, 60, 60),
                    listBounds.X,
                    listBounds.Y,
                    listBounds.Width,
                    listBounds.Height,
                    Color.White,
                    1f,
                    false);

                for (int row = 0; row < visibleRows; row++)
                {
                    int optionIndex = scrollIndex + row;
                    if (optionIndex >= allNames.Count)
                        break;

                    string name = allNames[optionIndex];
                    bool isLocked = lockedNames.Contains(name);
                    bool isSelected = selectedNames.Contains(name);
                    Rectangle rowBounds = GetRowBounds(row);

                    IClickableMenu.drawTextureBox(
                        b,
                        Game1.menuTexture,
                        new Rectangle(0, 256, 60, 60),
                        rowBounds.X,
                        rowBounds.Y,
                        rowBounds.Width,
                        rowBounds.Height,
                        isSelected ? new Color(220, 241, 220) : new Color(245, 245, 245),
                        1f,
                        false);

                    string checkbox = isSelected ? "[X]" : "[ ]";
                    string lockSuffix = isLocked ? " (locked)" : string.Empty;
                    Utility.drawTextWithShadow(
                        b,
                        $"{checkbox} {name}{lockSuffix}",
                        Game1.smallFont,
                        new Vector2(rowBounds.X + 12, rowBounds.Y + 10),
                        Game1.textColor);
                }

                if (allNames.Count > visibleRows)
                {
                    DrawSimpleButton(b, upButtonBounds, "^", new Color(232, 232, 232));
                    DrawSimpleButton(b, downButtonBounds, "v", new Color(232, 232, 232));
                }

                DrawSimpleButton(b, cancelButtonBounds, "Cancel", new Color(242, 218, 218));
                DrawSimpleButton(b, backButtonBounds, "Back", new Color(225, 225, 225));
                DrawSimpleButton(b, confirmButtonBounds, "Confirm", new Color(196, 236, 196));

                if (!string.IsNullOrWhiteSpace(validationMessage))
                {
                    Utility.drawTextWithShadow(
                        b,
                        validationMessage,
                        Game1.smallFont,
                        new Vector2(xPositionOnScreen + 58, yPositionOnScreen + height - 108),
                        Color.IndianRed);
                }

                drawMouse(b);
            }

            private Rectangle GetRowBounds(int row)
            {
                return new Rectangle(
                    listBounds.X + 8,
                    listBounds.Y + 8 + (row * rowHeight),
                    listBounds.Width - 16,
                    rowHeight - 4);
            }

            private void ToggleSelection(string name)
            {
                if (lockedNames.Contains(name))
                {
                    Game1.playSound("cancel");
                    validationMessage = $"{name} is required and cannot be removed.";
                    return;
                }

                validationMessage = string.Empty;
                if (selectedNames.Contains(name))
                {
                    selectedNames.Remove(name);
                    Game1.playSound("drumkit6");
                    return;
                }

                int optionalSelected = selectedNames.Count(selectedName => !lockedNames.Contains(selectedName));
                if (optionalSelected >= optionalLimit)
                {
                    validationMessage = optionalLimit == 0
                        ? "This location has no open attendee slots."
                        : $"You can add up to {optionalLimit} optional NPC(s).";
                    Game1.playSound("cancel");
                    return;
                }

                selectedNames.Add(name);
                Game1.playSound("smallSelect");
            }

            private void TryConfirm()
            {
                List<string> result = allNames.Where(name => selectedNames.Contains(name)).ToList();
                if (lockedNames.Any(lockedName => !selectedNames.Contains(lockedName)))
                {
                    validationMessage = "Main and required NPCs must remain selected.";
                    Game1.playSound("cancel");
                    return;
                }

                Game1.playSound("smallSelect");
                onConfirm(result);
            }
        }

        private static void DrawSimpleButton(SpriteBatch b, Rectangle bounds, string text, Color boxColor)
        {
            IClickableMenu.drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                bounds.X,
                bounds.Y,
                bounds.Width,
                bounds.Height,
                boxColor,
                1f,
                false);

            Vector2 textSize = Game1.smallFont.MeasureString(text);
            Vector2 textPosition = new Vector2(
                bounds.X + (bounds.Width - textSize.X) / 2f,
                bounds.Y + (bounds.Height - textSize.Y) / 2f + 2f);

            Utility.drawTextWithShadow(b, text, Game1.smallFont, textPosition, Game1.textColor);
        }

        private static bool RequiresLocationAndNpcSelection(string eventType)
        {
            return string.Equals(eventType, "Birthday", StringComparison.OrdinalIgnoreCase)
                || string.Equals(eventType, "PlayerBirthday", StringComparison.OrdinalIgnoreCase)
                || string.Equals(eventType, "Picnic", StringComparison.OrdinalIgnoreCase)
                || string.Equals(eventType, "Campfire", StringComparison.OrdinalIgnoreCase);
        }

        private static void OpenTimeSelectionMenu(
            string eventNpcName,
            string npcDisplayName,
            string eventType,
            string? npcResponseTemplate,
            string? prefilledTime,
            string? preselectedLocation,
            List<string>? preselectedNpcNames)
        {
            bool needsExtraSelection = RequiresLocationAndNpcSelection(eventType);
            Game1.activeClickableMenu = new EventTimeEntryMenu(
                npcDisplayName,
                eventType,
                onConfirm: normalizedEventTime =>
                {
                    if (!needsExtraSelection)
                    {
                        TryFinalizeSchedule(eventNpcName, npcDisplayName, eventType, npcResponseTemplate, normalizedEventTime, null, null);
                        return;
                    }

                    OpenLocationSelectionMenu(
                        eventNpcName,
                        npcDisplayName,
                        eventType,
                        npcResponseTemplate,
                        normalizedEventTime,
                        preselectedLocation,
                        preselectedNpcNames);
                },
                onCancel: () =>
                {
                    Game1.activeClickableMenu = null;
                },
                initialTimeText: prefilledTime,
                primaryActionLabel: needsExtraSelection ? "Next" : "Confirm");
        }

        private static void OpenLocationSelectionMenu(
            string eventNpcName,
            string npcDisplayName,
            string eventType,
            string? npcResponseTemplate,
            string normalizedEventTime,
            string? preselectedLocation,
            List<string>? preselectedNpcNames)
        {
            if (!TryGetEventLocationOptions(eventType, eventNpcName, out List<EventLocationOption> locationOptions))
            {
                Game1.playSound("cancel");
                Game1.addHUDMessage(new HUDMessage("No valid location is available for this event.", 3));
                Game1.activeClickableMenu = null;
                return;
            }

            Game1.activeClickableMenu = new EventLocationSelectionMenu(
                npcDisplayName,
                eventType,
                locationOptions,
                preselectedLocation,
                onNext: selectedLocation =>
                {
                    OpenNpcSelectionMenu(
                        eventNpcName,
                        npcDisplayName,
                        eventType,
                        npcResponseTemplate,
                        normalizedEventTime,
                        selectedLocation,
                        preselectedNpcNames);
                },
                onBack: () =>
                {
                    OpenTimeSelectionMenu(
                        eventNpcName,
                        npcDisplayName,
                        eventType,
                        npcResponseTemplate,
                        normalizedEventTime,
                        preselectedLocation,
                        preselectedNpcNames);
                },
                onCancel: () =>
                {
                    Game1.activeClickableMenu = null;
                });
        }

        private static void OpenNpcSelectionMenu(
            string eventNpcName,
            string npcDisplayName,
            string eventType,
            string? npcResponseTemplate,
            string normalizedEventTime,
            string selectedLocation,
            List<string>? preselectedNpcNames)
        {
            EventNpcSelectionInfo info = BuildNpcSelectionInfo(eventNpcName, eventType, selectedLocation);
            Game1.activeClickableMenu = new EventNpcSelectionMenu(
                npcDisplayName,
                eventType,
                info,
                preselectedNpcNames ?? info.LockedNames.ToList(),
                onConfirm: selectedNpcNames =>
                {
                    TryFinalizeSchedule(
                        eventNpcName,
                        npcDisplayName,
                        eventType,
                        npcResponseTemplate,
                        normalizedEventTime,
                        selectedLocation,
                        selectedNpcNames);
                },
                onBack: () =>
                {
                    OpenLocationSelectionMenu(
                        eventNpcName,
                        npcDisplayName,
                        eventType,
                        npcResponseTemplate,
                        normalizedEventTime,
                        selectedLocation,
                        preselectedNpcNames ?? info.LockedNames.ToList());
                },
                onCancel: () =>
                {
                    Game1.activeClickableMenu = null;
                });
        }

        private static bool TryGetEventLocationOptions(string eventType, string eventNpcName, out List<EventLocationOption> options)
        {
            options = new List<EventLocationOption>();
            if (string.Equals(eventType, "Birthday", StringComparison.OrdinalIgnoreCase))
            {
                HashSet<string> allowedBirthdayKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                NPC? eventNpc = Game1.getCharacterFromName(eventNpcName);
                if (!string.IsNullOrWhiteSpace(eventNpc?.DefaultMap) && birthdayMap.TryGetValue(eventNpc.DefaultMap, out _))
                    allowedBirthdayKeys.Add(eventNpc.DefaultMap);

                if (birthdayMap.TryGetValue("Saloon", out _))
                    allowedBirthdayKeys.Add("Saloon");

                if (Game1.MasterPlayer.hasCompletedCommunityCenter() && birthdayMap.TryGetValue("CommunityCenter", out _))
                    allowedBirthdayKeys.Add("CommunityCenter");

                foreach (string key in allowedBirthdayKeys)
                {
                    BirthdayMapData data = birthdayMap[key];
                    if (data is null || string.IsNullOrWhiteSpace(data.event_map))
                        continue;

                    if (Game1.getLocationFromName(data.event_map) == null)
                        continue;

                    options.Add(new EventLocationOption
                    {
                        Key = key,
                        DisplayName = Game1.getLocationFromName(data.event_map)?.DisplayName ?? data.event_map,
                        MaxParticipants = data.npc_tiles?.Count ?? 0,
                        RequiredNpcNames = data.required_npc?.Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>()
                    });
                }
            }
            else if (string.Equals(eventType, "PlayerBirthday", StringComparison.OrdinalIgnoreCase))
            {
                if (birthdayMap.TryGetValue("Saloon", out BirthdayMapData saloonData)
                    && saloonData != null && !string.IsNullOrWhiteSpace(saloonData.event_map)
                    && Game1.getLocationFromName(saloonData.event_map) != null)
                {
                    options.Add(new EventLocationOption
                    {
                        Key = "Saloon",
                        DisplayName = Game1.getLocationFromName(saloonData.event_map)?.DisplayName ?? saloonData.event_map,
                        MaxParticipants = saloonData.npc_tiles?.Count ?? 0,
                        RequiredNpcNames = saloonData.required_npc?.Where(name => !string.IsNullOrWhiteSpace(name))
                            .Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>()
                    });
                }

                if (Game1.MasterPlayer.hasCompletedCommunityCenter()
                    && birthdayMap.TryGetValue("CommunityCenter", out BirthdayMapData ccData)
                    && ccData != null && !string.IsNullOrWhiteSpace(ccData.event_map)
                    && Game1.getLocationFromName(ccData.event_map) != null)
                {
                    options.Add(new EventLocationOption
                    {
                        Key = "CommunityCenter",
                        DisplayName = Game1.getLocationFromName(ccData.event_map)?.DisplayName ?? ccData.event_map,
                        MaxParticipants = ccData.npc_tiles?.Count ?? 0,
                        RequiredNpcNames = ccData.required_npc?.Where(name => !string.IsNullOrWhiteSpace(name))
                            .Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>()
                    });
                }
            }
            else if (string.Equals(eventType, "Picnic", StringComparison.OrdinalIgnoreCase))
            {
                foreach ((string key, PicnicMapData data) in picnicMap)
                {
                    if (data is null || Game1.getLocationFromName(key) == null)
                        continue;

                    options.Add(new EventLocationOption
                    {
                        Key = key,
                        DisplayName = Game1.getLocationFromName(key)?.DisplayName ?? key,
                        MaxParticipants = data.npc_tile != null ? 1 : 0
                    });
                }
            }
            else if (string.Equals(eventType, "Campfire", StringComparison.OrdinalIgnoreCase))
            {
                foreach ((string key, CampfireMapData data) in campfireMap)
                {
                    if (data is null || Game1.getLocationFromName(key) == null)
                        continue;

                    options.Add(new EventLocationOption
                    {
                        Key = key,
                        DisplayName = Game1.getLocationFromName(key)?.DisplayName ?? key,
                        MaxParticipants = data.npc_tiles?.Count ?? 0
                    });
                }
            }

            options = options.OrderBy(option => option.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
            return options.Count > 0;
        }

        private static EventNpcSelectionInfo BuildNpcSelectionInfo(string eventNpcName, string eventType, string selectedLocation)
        {
            EventNpcSelectionInfo info = new EventNpcSelectionInfo();
            List<string> lockedNames = GetDefaultLockedNpcNames(eventNpcName, eventType, selectedLocation);
            foreach (string lockedName in lockedNames)
                info.LockedNames.Add(lockedName);

            int maxParticipants = 0;
            if (string.Equals(eventType, "Birthday", StringComparison.OrdinalIgnoreCase) && birthdayMap.TryGetValue(selectedLocation, out BirthdayMapData? birthdayData))
                maxParticipants = birthdayData?.npc_tiles?.Count ?? 0;
            else if (string.Equals(eventType, "PlayerBirthday", StringComparison.OrdinalIgnoreCase) && birthdayMap.TryGetValue(selectedLocation, out BirthdayMapData? playerBirthdayData))
                maxParticipants = playerBirthdayData?.npc_tiles?.Count ?? 0;
            else if (string.Equals(eventType, "Campfire", StringComparison.OrdinalIgnoreCase) && campfireMap.TryGetValue(selectedLocation, out CampfireMapData? campfireData))
                maxParticipants = campfireData?.npc_tiles?.Count ?? 0;
            else if (string.Equals(eventType, "Picnic", StringComparison.OrdinalIgnoreCase) && picnicMap.TryGetValue(selectedLocation, out PicnicMapData? picnicData))
                maxParticipants = picnicData?.npc_tile != null ? 1 : 0;

            int lockedForCapacity = lockedNames.Count;
            if (string.Equals(eventType, "Birthday", StringComparison.OrdinalIgnoreCase) && lockedNames.Any(name => string.Equals(name, eventNpcName, StringComparison.OrdinalIgnoreCase)))
                lockedForCapacity -= 1;

            // For PlayerBirthday the player occupies host_tile, not npc_tiles, so no subtraction needed.
            info.OptionalLimit = Math.Max(0, maxParticipants - Math.Max(0, lockedForCapacity));

            List<string> lockedOrdered = lockedNames
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            List<string> optionalOrdered = GetSelectableNpcPool()
                .Where(candidate => !lockedNames.Any(name => string.Equals(name, candidate, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            info.AllNames = lockedOrdered.Concat(optionalOrdered).ToList();
            return info;
        }

        private static List<string> GetDefaultLockedNpcNames(string eventNpcName, string eventType, string selectedLocation)
        {
            List<string> lockedNames = new List<string>();
            if (!string.IsNullOrWhiteSpace(eventNpcName))
                lockedNames.Add(eventNpcName.Trim());

            if (string.Equals(eventType, "Birthday", StringComparison.OrdinalIgnoreCase)
                && birthdayMap.TryGetValue(selectedLocation, out BirthdayMapData? birthdayData)
                && birthdayData?.required_npc != null)
            {
                foreach (string requiredName in birthdayData.required_npc)
                {
                    if (!string.IsNullOrWhiteSpace(requiredName))
                        lockedNames.Add(requiredName.Trim());
                }
            }

            if (string.Equals(eventType, "PlayerBirthday", StringComparison.OrdinalIgnoreCase)
                && birthdayMap.TryGetValue(selectedLocation, out BirthdayMapData? playerBirthdayData)
                && playerBirthdayData?.required_npc != null)
            {
                foreach (string requiredName in playerBirthdayData.required_npc)
                {
                    if (!string.IsNullOrWhiteSpace(requiredName))
                        lockedNames.Add(requiredName.Trim());
                }
            }

            return lockedNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<string> GetSelectableNpcPool()
        {
            return Utility.getAllVillagers()
                .Where(npc => npc != null
                    && !socialNpcBlacklist.Contains(npc.Name)
                    && !npc.IsInvisible
                    && npc.CanSocialize
                    && Game1.player.friendshipData.TryGetValue(npc.Name, out Friendship? friendship)
                    && friendship.Points >= 250)
                .Select(npc => npc.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void TryFinalizeSchedule(
            string eventNpcName,
            string npcDisplayName,
            string eventType,
            string? npcResponseTemplate,
            string normalizedEventTime,
            string? selectedLocation,
            List<string>? selectedNpcNames)
        {
            bool isNewSchedule = TryAddPendingUnlimitedEvent(
                eventNpcName,
                eventType,
                normalizedEventTime,
                selectedLocation,
                selectedNpcNames);

            Game1.activeClickableMenu = null;
            if (!isNewSchedule)
            {
                Game1.addHUDMessage(new HUDMessage(TimeSlotUnavailableMessage, 3));
                return;
            }

            string displayTime = FormatEventTimeForDisplay(normalizedEventTime);
            string locationLabel = string.IsNullOrWhiteSpace(selectedLocation)
                ? string.Empty
                : $" at {(Game1.getLocationFromName(selectedLocation)?.DisplayName ?? selectedLocation)}";
            string feedback = string.IsNullOrWhiteSpace(eventNpcName)
                ? $"Scheduled {eventType} at {displayTime}{locationLabel}."
                : $"Scheduled {eventType} with {npcDisplayName} at {displayTime}{locationLabel}.";

            if (!string.IsNullOrWhiteSpace(npcResponseTemplate) && !string.IsNullOrWhiteSpace(eventNpcName))
                iAppMessengerApi.SendSmartphoneMessageFromNPC(eventNpcName, npcResponseTemplate);

            iSmartphoneApi.SendSmartphoneNotification(feedback, "Unlimited Events Expansion");
        }

        public static void TryOpenScheduleEventTimeMenu(
        string eventNpcName,
        string eventType,
        string? npcResponseTemplate)
        {
            if (Game1.getCharacterFromName(eventNpcName) is not NPC eventNpc)
            {
                Game1.addHUDMessage(new HUDMessage("NPC not found: " + eventNpcName, 3));
                return;
            }

            if ((Game1.isRaining || Game1.isGreenRain || Game1.isLightning) && (eventType == "Campfire" || eventType == "Picnic"))
            {
                Game1.playSound("cancel");
                iSmartphoneApi.SendSmartphoneNotification("Cannot schedule outdoor events in this weather.", "Unlimited Events Expansion");
                return;
            }

            if (string.IsNullOrWhiteSpace(Config.Key) && TotalEventRegisteredToday >= ModEntry.DailyEventLimit)
            {
                Game1.playSound("cancel");
                iSmartphoneApi.SendSmartphoneNotification($"You can only schedule {ModEntry.DailyEventLimit} events per day without your own API key. Check out the mod page for more instructions!", "Unlimited Events Expansion");
                return;
            }

            if (!TryGetMinimumAllowedEventTime(out _))
            {
                Game1.playSound("cancel");
                Game1.addHUDMessage(new HUDMessage(TooLateToScheduleMessage, 3));
                return;
            }

            string npcDisplayName = eventNpc.displayName;
            OpenTimeSelectionMenu(eventNpcName, npcDisplayName, eventType, npcResponseTemplate, null, null, null);
        }

        public static void TryOpenSchedulePlayerBirthdayMenu()
        {
            if (!TryGetMinimumAllowedEventTime(out _))
            {
                Game1.playSound("cancel");
                Game1.addHUDMessage(new HUDMessage(TooLateToScheduleMessage, 3));
                return;
            }

            string playerDisplayName = Game1.player.Name;
            OpenTimeSelectionMenu("", playerDisplayName, "PlayerBirthday", null, null, null, null);
        }

        private static bool TryAddPendingUnlimitedEvent(
            string npcName,
            string eventType,
            string normalizedEventTime,
            string? selectedLocation,
            List<string>? selectedParticipants)
        {
            string normalizedScheduledTime = normalizedEventTime.Trim();
            if (IsEventTimeAlreadyScheduled(normalizedScheduledTime))
                return false;

            string normalizedNpcName = npcName.Trim();
            string normalizedEventType = eventType.Trim();
            string? normalizedLocation = string.IsNullOrWhiteSpace(selectedLocation) ? null : selectedLocation.Trim();
            List<string> normalizedParticipants = (selectedParticipants ?? new List<string>())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            bool alreadyExists = PendingUnlimitedEvents.Any(existing =>
                string.Equals(existing.NpcName, normalizedNpcName, StringComparison.Ordinal)
                && string.Equals(existing.EventType, normalizedEventType, StringComparison.Ordinal)
                && string.Equals(existing.TimeOfDay, normalizedScheduledTime, StringComparison.Ordinal)
                && string.Equals(existing.LocationName ?? string.Empty, normalizedLocation ?? string.Empty, StringComparison.Ordinal)
                && existing.ParticipantNames.SequenceEqual(normalizedParticipants));

            if (alreadyExists)
                return false;

            PendingUnlimitedEvents.Add(new ScheduledUnlimitedEvent
            {
                NpcName = normalizedNpcName,
                EventType = normalizedEventType,
                TimeOfDay = normalizedScheduledTime,
                LocationName = normalizedLocation,
                ParticipantNames = normalizedParticipants
            });
            if (!string.Equals(normalizedEventType, "PlayerBirthday", StringComparison.OrdinalIgnoreCase))
                TotalEventRegisteredToday++;
            return true;
        }

        private static string FormatEventTimeForDisplay(string normalizedEventTime)
        {
            if (!int.TryParse(normalizedEventTime, out int parsedTime))
                return normalizedEventTime ?? string.Empty;

            int hour24 = (parsedTime / 100) % 24;
            int minute = parsedTime % 100;

            string period = hour24 >= 12 ? "PM" : "AM";
            int hour12 = hour24 % 12;
            if (hour12 == 0)
                hour12 = 12;

            return $"{hour12}:{minute:00} {period}";
        }


        private static bool TryNormalizeEventTime(string? eventTime, out string normalizedTime)
        {
            normalizedTime = string.Empty;
            if (string.IsNullOrWhiteSpace(eventTime))
                return false;

            if (!int.TryParse(eventTime.Trim(), out int parsedTime))
                return false;

            int hour = parsedTime / 100;
            int minute = parsedTime % 100;

            // Validation: 6am through 10:59pm.
            if (hour < 6 || hour > 23 || minute > 59)
                return false;

            // Round the minutes down to the nearest 10 (e.g., 15 becomes 10)
            int normalizedMinute = (minute / 10) * 10;

            // Reconstruct the time (e.g., 600 + 10 = 610).
            int finalTime = (hour * 100) + normalizedMinute;
            if (finalTime > 2300)
                return false;

            normalizedTime = $"{finalTime:0000}";
            return true;
        }

        private static bool TryValidateEventTimeSelection(string? eventTime, out string normalizedTime, out string validationError)
        {
            validationError = string.Empty;
            normalizedTime = string.Empty;

            string digitsOnly = new string((eventTime ?? string.Empty).Where(char.IsDigit).ToArray());
            if (!TryNormalizeEventTime(digitsOnly, out normalizedTime))
            {
                validationError = "Please enter a valid time in HHMM between 0600 and 2300.";
                return false;
            }

            if (!TryGetMinimumAllowedEventTime(out string minimumAllowedTime))
            {
                validationError = TooLateToScheduleMessage;
                return false;
            }

            if (!int.TryParse(normalizedTime, out int selectedTime)
                || !int.TryParse(minimumAllowedTime, out int minimumTime))
            {
                validationError = "Unable to validate the selected time.";
                return false;
            }

            if (selectedTime < minimumTime)
            {
                validationError = $"Choose {FormatEventTimeForDisplay(minimumAllowedTime)} or later.";
                return false;
            }

            if (IsEventTimeAlreadyScheduled(normalizedTime))
            {
                validationError = TimeSlotUnavailableMessage;
                return false;
            }

            return true;
        }

        private static bool IsEventTimeAlreadyScheduled(string normalizedEventTime)
        {
            if (string.IsNullOrWhiteSpace(normalizedEventTime))
                return false;

            string trimmedTime = normalizedEventTime.Trim();
            return PendingUnlimitedEvents.Any(scheduledEvent => string.Equals(scheduledEvent.TimeOfDay.Trim(), trimmedTime, StringComparison.Ordinal));
        }

        private static bool TryGetMinimumAllowedEventTime(out string normalizedMinimumAllowedTime)
        {
            normalizedMinimumAllowedTime = string.Empty;
            if (!TryGetAllowedEventTimeWindow(out int minimumAllowedMinutes, out _))
                return false;

            int minimumAllowedTime = ((minimumAllowedMinutes / 60) * 100) + (minimumAllowedMinutes % 60);
            normalizedMinimumAllowedTime = $"{minimumAllowedTime:0000}";
            return true;
        }

        private static bool TryGetAllowedEventTimeWindow(out int minimumAllowedMinutes, out int latestAllowedMinutes)
        {
            const int earliestEventMinutes = 6 * 60;
            const int minimumLeadMinutes = 2 * 60;

            minimumAllowedMinutes = 0;
            latestAllowedMinutes = 23 * 60;

            int currentTotalMinutes = GetCurrentGameClockTotalMinutes();
            int leadAdjustedMinimum = RoundUpToNearestTenMinutes(currentTotalMinutes + minimumLeadMinutes);
            minimumAllowedMinutes = Math.Max(earliestEventMinutes, leadAdjustedMinimum);

            return minimumAllowedMinutes <= latestAllowedMinutes;
        }

        private static int GetCurrentGameClockTotalMinutes()
        {
            int hour = Game1.timeOfDay / 100;
            int minute = Math.Clamp(Game1.timeOfDay % 100, 0, 59);
            return (hour * 60) + minute;
        }

        private static int RoundUpToNearestTenMinutes(int totalMinutes)
        {
            return ((totalMinutes + 9) / 10) * 10;
        }

    }
}
