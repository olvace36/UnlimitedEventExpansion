using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using UnlimitedEventExpansion;
using StardewModdingAPI;

namespace UnlimitedEventExpansion
{
    public partial class ModEntry
    {
        private const string TooLateToScheduleMessage = "Too late to schedule this event today.";
        private const string TimeSlotUnavailableMessage = "Cannot schedule at this time";

        private sealed class EventTimeEntryMenu : IClickableMenu
        {
            private readonly Action<string> onConfirm;
            private readonly Action onCancel;
            private readonly string npcDisplayName;
            private readonly string eventDisplayName;

            private readonly TextBox timeTextBox;
            private readonly Rectangle textBoxBounds;
            private readonly Rectangle minusButtonBounds;
            private readonly Rectangle plusButtonBounds;
            private readonly Rectangle okButtonBounds;
            private readonly Rectangle cancelButtonBounds;

            private string validationMessage = string.Empty;

            public EventTimeEntryMenu(
                string npcDisplayName,
                string eventDisplayName,
                Action<string> onConfirm,
                Action onCancel)
                : base(Game1.uiViewport.Width / 2 - 260, Game1.uiViewport.Height / 2 - 170, 520, 340, showUpperRightCloseButton: false)
            {
                this.onConfirm = onConfirm ?? (_ => { });
                this.onCancel = onCancel ?? (() => { });
                this.npcDisplayName = string.IsNullOrWhiteSpace(npcDisplayName) ? "NPC" : npcDisplayName;
                this.eventDisplayName = string.IsNullOrWhiteSpace(eventDisplayName) ? "event" : eventDisplayName;

                textBoxBounds = new Rectangle(xPositionOnScreen + 105, yPositionOnScreen + 150, 200, 48);
                minusButtonBounds = new Rectangle(xPositionOnScreen + 45, yPositionOnScreen + 150, 48, 48);
                plusButtonBounds = new Rectangle(xPositionOnScreen + 317, yPositionOnScreen + 150, 48, 48);
                okButtonBounds = new Rectangle(xPositionOnScreen + 105, yPositionOnScreen + 245, 120, 56);
                cancelButtonBounds = new Rectangle(xPositionOnScreen + 245, yPositionOnScreen + 245, 120, 56);

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
                    Text = BuildDefaultSuggestedTime()
                };
                timeTextBox.textLimit = 5;
                timeTextBox.Selected = true;
                Game1.keyboardDispatcher.Subscriber = timeTextBox;

                if (!TryGetMinimumAllowedEventTime(out _))
                    validationMessage = TooLateToScheduleMessage;
            }

            public override void update(GameTime time)
            {
                base.update(time);
                timeTextBox.Update();
            }

            public override void receiveLeftClick(int x, int y, bool playSound = true)
            {
                if (okButtonBounds.Contains(x, y))
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

                string title = $"Schedule {eventDisplayName} with {npcDisplayName}";
                Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
                Vector2 titlePosition = new Vector2(xPositionOnScreen + (width - titleSize.X) / 2f, yPositionOnScreen + 48f);
                Utility.drawTextWithShadow(b, title, Game1.dialogueFont, titlePosition, Game1.textColor);
                TryGetMinimumAllowedEventTime(out string minimumAllowedPreview);
                Utility.drawTextWithShadow(
                    b,
                    $"Enter HHMM ({FormatEventTimeForDisplay(minimumAllowedPreview)} - 23:00)",
                    Game1.smallFont,
                    new Vector2(xPositionOnScreen + 45, yPositionOnScreen + 108),
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
                        new Vector2(xPositionOnScreen + 45, yPositionOnScreen + 212),
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
                            new Vector2(xPositionOnScreen + 45, yPositionOnScreen + 212),
                            Color.IndianRed);
                    }
                    else
                    {
                        Utility.drawTextWithShadow(
                        b,
                        "Selected: Invalid time",
                        Game1.smallFont,
                        new Vector2(xPositionOnScreen + 45, yPositionOnScreen + 212),
                        Color.IndianRed);
                    }
                }

                if (string.IsNullOrEmpty(minimumAllowedPreview))
                {
                    Utility.drawTextWithShadow(
                        b,
                        TooLateToScheduleMessage,
                        Game1.smallFont,
                        new Vector2(xPositionOnScreen + 45, yPositionOnScreen + 228),
                        Color.IndianRed);
                }

                DrawButton(b, okButtonBounds, "OK", new Color(196, 236, 196));
                DrawButton(b, cancelButtonBounds, "Cancel", new Color(242, 218, 218));

                if (!string.IsNullOrWhiteSpace(validationMessage))
                {
                    Utility.drawTextWithShadow(
                        b,
                        validationMessage,
                        Game1.smallFont,
                        new Vector2(xPositionOnScreen + 45, yPositionOnScreen + 305),
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
                if (Game1.keyboardDispatcher.Subscriber == timeTextBox)
                    Game1.keyboardDispatcher.Subscriber = null;

                timeTextBox.Selected = false;
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

                if (Game1.keyboardDispatcher.Subscriber == timeTextBox)
                    Game1.keyboardDispatcher.Subscriber = null;

                timeTextBox.Selected = false;
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

        }



        public static void TryOpenScheduleEventTimeMenu(
        string eventNpcName,
        string eventType,
        string eventDisplayName,
        string npcDisplayName,
        string? npcResponseTemplate)
        {
            if (!TryGetMinimumAllowedEventTime(out _))
            {
                Game1.playSound("cancel");
                Game1.addHUDMessage(new HUDMessage(TooLateToScheduleMessage, 3));
                return;
            }

            Game1.activeClickableMenu = new EventTimeEntryMenu(
                npcDisplayName,
                eventDisplayName,
                onConfirm: normalizedEventTime =>
                {
                    bool isNewSchedule = TryAddPendingUnlimitedEvent(eventNpcName, eventType, normalizedEventTime);

                    Game1.activeClickableMenu = null;

                    if (!isNewSchedule)
                    {
                        Game1.addHUDMessage(new HUDMessage(TimeSlotUnavailableMessage, 3));
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(npcResponseTemplate))
                    {
                        iSmartPhoneApi.SendSmartphoneMessageFromNPC(eventNpcName, $"{npcDisplayName}: {npcResponseTemplate}");
                    }
                    else
                    {
                        string displayTime = FormatEventTimeForDisplay(normalizedEventTime);
                        string feedback = $"Scheduled {eventDisplayName} with {npcDisplayName} at {displayTime}.";
                        Game1.addHUDMessage(new HUDMessage(feedback, 2));
                    }
                },
                onCancel: () =>
                {
                    Game1.activeClickableMenu = null;
                });
        }

        private static bool TryAddPendingUnlimitedEvent(string npcName, string eventType, string normalizedEventTime)
        {
            string normalizedScheduledTime = normalizedEventTime.Trim();
            if (IsEventTimeAlreadyScheduled(normalizedScheduledTime))
                return false;

            var pendingEvent = (npcName.Trim(), eventType.Trim(), normalizedScheduledTime);
            if (PendingUnlimitedEvents.Contains(pendingEvent))
                return false;

            PendingUnlimitedEvents.Add(pendingEvent);
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
