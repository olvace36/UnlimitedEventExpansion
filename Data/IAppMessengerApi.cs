using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace UnlimitedEventExpansion
{
    public interface IAppMessengerApi
    {
        /// ======================================
        /// API for interacting with the smartphone messenger app
        /// ======================================

        /// <summary>
        /// Sends a message from an NPC to the player. This method is used to simulate receiving messages on the player's smartphone from NPCs in the game. Nothing will happen if the specified NPC is not in the messenger app list.
        /// </summary>
        /// <param name="npcName">The name of the NPC sending the message (case-sensitive).</param>
        /// <param name="message">The content of the message being sent.</param>
        /// <param name="playerId">(optional) The target player's UniqueMultiplayerID as string. If null/empty/invalid, this is broadcast to all online players.</param>
        void SendSmartphoneMessageFromNPC(string npcName, string message, string playerId = "");




        ///// ======================================
        ///// API to get player profile
        ///// ======================================

        ///// <summary>
        ///// Gets the player's profile information as a string.
        ///// </summary> 
        ///// <returns>A string representing the player's profile information.</returns>
        //string GetPlayerProfile();

        ///// <summary>
        ///// Gets the player's birth date as a string.
        ///// </summary>
        ///// <returns>A string representing the player's birth date.</returns>
        //string GetPlayerBirthDate();

        ///// <summary>
        ///// Gets the player's birth season as a string.
        ///// </summary>
        ///// <returns>A string representing the player's birth season.</returns>
        //string GetPlayerBirthSeason();

        ///// <summary>
        ///// Gets the player's age as a string.
        ///// </summary>
        ///// <returns>A string representing the player's age.</returns>
        //string GetPlayerAge();





        /// ======================================
        /// API for Unlimited Event Expansion only
        /// ======================================

        /// <summary>
        /// Registers or updates an event type that can be suggested by AI chat and scheduled through Smartphone.
        /// The <paramref name="eventType"/> value is used as the tool enum value, so keep it stable.
        /// </summary>
        /// <param name="ownerModId">The unique ID of the mod that owns this event registration.</param>
        /// <param name="eventType">Unique event key shown to the AI tool (for example: "Birthday").</param>
        /// <param name="triggerEvent">Callback invoked when Smartphone triggers this event for an NPC name.</param>
        /// <param name="minimumHeartLevel">Minimum heart level required before this event is exposed to AI tools.</param>
        /// <param name="toolDescription">Optional extra context appended to the Schedule_Event tool description.</param>
        /// <returns>True if registration succeeded; otherwise false.</returns>
        bool RegisterUnlimitedEvent(
            string ownerModId,
            string eventType,
            Action<string> triggerEvent,
            int minimumHeartLevel = 0,
            string toolDescription = ""
        );

        /// <summary>
        /// Unregisters a previously registered event type.
        /// </summary>
        /// <param name="ownerModId">The unique ID of the mod that owns this event registration.</param>
        /// <param name="eventType">The event key that was used during registration.</param>
        /// <returns>True if an event type was removed; otherwise false.</returns>
        bool UnregisterUnlimitedEvent(string ownerModId, string eventType);

    }
}
