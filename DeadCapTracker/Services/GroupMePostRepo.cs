using DeadCapTracker.Models.BotModels;
using DeadCapTracker.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace DeadCapTracker.Services
{
    public interface IGroupMePostRepo
    {
        Task BotPost(string text);
        Task BotPostWithTag(string text, string nickname, string memberId);
        Task<GroupParent> GetMemberIds();
    }

    public class GroupMePostRepo : IGroupMePostRepo
    {
        public IGroupMeApi _gmApi { get; }

        public GroupMePostRepo(IGroupMeApi gmApi)
        {
            _gmApi = gmApi;
        }


        public async Task BotPost(string text)
        {
            var message = new Message(text);
            try
            {
                await _gmApi.SendMessage(message);
            }
            catch (HttpRequestException e)
            {

                Console.WriteLine(e.Message);
                Console.WriteLine(e.InnerException);
            }
            
        }

        public async Task BotPostWithTag(string text, string nickname, string memberId)
        {
            var rawText = $"{nickname}{text}";
            var message = new Message(rawText);
            var mention = new Mention { type = "mentions" };
            int[][] locis = new int[1][] { new[] { 0, nickname.Length } };
            var mentionIds = new[] { memberId };
            mention.loci = locis;
            mention.user_ids = mentionIds;
            var mentionList = new List<Mention> { mention };
            message.attachments = mentionList;
            await _gmApi.SendMessage(message);
        }

        public async Task<GroupParent> GetMemberIds()
        {
            return await _gmApi.GetMemberIds();
        }
    }
}
