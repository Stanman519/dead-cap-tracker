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
        Task BotPost(string text, bool isError = false);
        Task BotPostWithTag(string text, string nickname, string memberId);
        Task<GroupParent> GetMemberIds();
    }

    public class GroupMePostRepo : IGroupMePostRepo
    {
        private readonly ILogger<GroupMePostRepo> _logger;

        public IGroupMeApi _gmApi { get; }

        public GroupMePostRepo(IGroupMeApi gmApi, ILogger<GroupMePostRepo> logger)
        {
            _gmApi = gmApi;
            _logger = logger;
        }


        public async Task BotPost(string text, bool isError = false)
        {
            
            var message = isError ? new Message(text, "be51b03607ae680832dfd1c7c7") : new Message(text);
            try
            {
                await _gmApi.SendMessage(message);
            }
            catch (HttpRequestException e)
            {
                _logger.LogError("GroupMe Request Error", e);
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
            try
            {
                await _gmApi.SendMessage(message);
            }
            catch (HttpRequestException e)
            {
                _logger.LogError("GroupMe Tag Request Error", e);
            }
        }

        public async Task<GroupParent> GetMemberIds()
        {
            try
            {
                return await _gmApi.GetMemberIds();
            }
            catch (Exception e)
            {
                _logger.LogError("GroupMe memberId Request Error", e);
                return null;
            }

        }
    }
}
