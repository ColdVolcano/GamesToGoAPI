﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GamesToGo.API.GameExecution;
using GamesToGo.API.Models;

namespace GamesToGo.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RoomController : UserAwareController
    {
        private static int roomID;
        private static readonly List<Room> rooms = new List<Room>();

        public RoomController(GamesToGoContext context) : base(context)
        {
        }

        [HttpPost("CreateRoom")]
        public async Task<ActionResult<Room>> CreateRoom([FromForm] string gameID)
        {
            if (LoggedUser.Room != null)
                return Conflict($"Already joined, leave current room to create another one");
            Game game = await Context.Game.FindAsync(int.Parse(gameID));
            if (game == null)
                return BadRequest($"Game ID {gameID} not found");
            roomID++;
            Room cRoom = new Room(roomID, LoggedUser, game);
            rooms.Add(cRoom);
            return cRoom;
        }

        [HttpGet("AllRoomsFor/{id}")]
        public ActionResult<IEnumerable<RoomPreview>> RoomsForGameID(int id)
        {
            return rooms.Where(r => r.Game.Id == id).Select(r => (RoomPreview) r).ToList();
        }

        [HttpPost("JoinRoom")]
        public ActionResult<Room> JoinRoom([FromForm] string id)
        {
            if (LoggedUser.Room != null)
                return Conflict();
            Room jRoom = GetRoom(int.Parse(id));

            if (jRoom == null)
                return NotFound($"No such room");
            
            if (jRoom.HasStarted || !jRoom.JoinUser(LoggedUser))
                return BadRequest("Room is full or already started");
            
            return jRoom;
        }

        [HttpPost("LeaveRoom")]
        public ActionResult LeaveRoom()
        {
            Room targetRoom = LoggedUser.Room;
            if (targetRoom?.LeaveUser(LoggedUser) ?? false)
            {
                if (((RoomPreview) targetRoom).CurrentPlayers == 0)
                    rooms.Remove(targetRoom);
                return Ok();
            }
            return Conflict($"Haven't joined no room");
        }

        [HttpPost("Ready")]
        public ActionResult ReadyUser()
        {
            if (LoggedUser.Room?.ReadyUser(LoggedUser) ?? false)
                return Ok();
            return Conflict("Haven't joined no room");
        }

        [HttpGet("RoomState")]
        public ActionResult<Room> JoinedRoomState()
        {
            if (LoggedUser.Room == null)
                return BadRequest();

            return LoggedUser.Room;
        }

        public static Room GetRoom(int id)
        {
            return rooms.FirstOrDefault(x => x.ID == id);
        }
    }
}