using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace QuickTag
{
    public class QuickTag : GenericPlugin
    {
        private const string AppName = "QuickTag";

        private static readonly ILogger logger = LogManager.GetLogger();

        private QuickTagSettingsViewModel settings { get; set; }

        private readonly IList<MainMenuItem> _mainMenuItemDefaults;
        private readonly IList<GameMenuItem> _gameMenuItemDefaults;

    public override Guid Id { get; } = Guid.Parse("33c35592-96b5-43c2-b350-478f80dbdae5");

        public QuickTag(IPlayniteAPI api) : base(api)
        {
            settings = new QuickTagSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };

            _mainMenuItemDefaults = new List<MainMenuItem> { ConstructMainMenuItem(AddTagToMenu, "Add Tag To Quick Menu") };
            _gameMenuItemDefaults = new List<GameMenuItem> { ConstructGameMenuItem(AddNewTagToGames, "Add New Tag...") };
        }

        public override ISettings GetSettings(bool firstRunSettings) => settings;

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            foreach (var mainMenuItem in _mainMenuItemDefaults)
                yield return mainMenuItem;

            foreach (var tagText in Settings.Tags)
                yield return ConstructMainMenuItem(_ => RemoveTagFromMenu(tagText), tagText, "|Remove From Quick Menu");
        }
        
        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            foreach (var gameMenuItem in _gameMenuItemDefaults)
                yield return gameMenuItem;

            if (SelectedGames.Any())
            {
                foreach (var tag in Settings.Tags)
                    yield return ConstructGameMenuItem(_ => AddTagToGames(TextToTag(tag)), tag);

                var uniqueTags = SelectedGames.Where(g => g.Tags != null).SelectMany(g => g.Tags).Distinct().ToList();

                foreach (var tag in PlayniteApi.Database.Tags?.Where(t => !uniqueTags.Contains(t)))
                    yield return ConstructGameMenuItem(_ => AddTagToGames(tag), tag.Name, "|Add");

                foreach (var tag in uniqueTags)
                    yield return ConstructGameMenuItem(_ => RemoveTagFromGames(tag), tag.Name, "|Remove");
            }
        }

        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            var deletedTags = new List<string>();

            foreach (var tag in Settings.Tags) if (Tags.All(t => t.Name != tag))
                deletedTags.Add(tag);

            foreach(var tag in deletedTags)
                Settings.Tags.Remove(tag);
        }

        private GameMenuItem ConstructGameMenuItem(Action<GameMenuItemActionArgs> action, string desc, string subPath = "") => new GameMenuItem
        {
            Action = action,
            Description = desc,
            MenuSection = AppName + subPath
        };

        private MainMenuItem ConstructMainMenuItem(Action<MainMenuItemActionArgs> action, string desc, string subPath = "") => new MainMenuItem
        {
            Action = action,
            Description = desc,
            MenuSection = "@" + AppName + subPath
        };

        private void AddTagToMenu(MainMenuItemActionArgs _)
        {
            var tags = Tags.ToList().ConvertAll(t => new GenericItemOption { Name = t.Name });

            var newTag = Dialogs.ChooseItemWithSearch(tags,
                s => tags.OrderByDescending(t => t.Name.StartsWith(s)).ToList(), string.Empty, "Please select a tag...");
        
            if (newTag != null && !Settings.Tags.Contains(newTag.Name))
                Settings.Tags.Add(newTag.Name);
        }

        private void RemoveTagFromMenu(string tagText)
        {
            Settings.Tags.Remove(tagText);
            SavePluginSettings(Settings);
        }

        private void AddNewTagToGames(GameMenuItemActionArgs _)
        {
            var newTagResult = Dialogs.SelectString("QuickTag", "Please input a new tag...", string.Empty);
            if (newTagResult.Result) 
                AddTagToGames(Tags.Add(newTagResult.SelectedString));

            if (Settings.AddNewTagToQuickMenu)
            {
                Settings.Tags.Add(newTagResult.SelectedString);
                SavePluginSettings(Settings);
            }
        }

        private void AddTagToGames(Tag tag)
        {
            foreach (var game in SelectedGames)
            if (game.Tags == null)
            {
                game.TagIds = new List<Guid> { tag.Id };
                Games.Update(game);
            }
            else if (!game.TagIds.Contains(tag.Id))
            {
                game.TagIds.Add(tag.Id);
                Games.Update(game);
            }
        }

        private void RemoveTagFromGames(Tag tag)
        {
            foreach (var game in SelectedGames) if (game.Tags != null && game.TagIds.Remove(tag.Id))
                Games.Update(game);
        }

        private Tag TextToTag(string text) => Tags.First(t => t.Name == text);

        IItemCollection<Game> Games => PlayniteApi.Database.Games;
        private IEnumerable<Game> SelectedGames => PlayniteApi.MainView.SelectedGames;
        private QuickTagSettings Settings => settings.Settings;
        private IDialogsFactory Dialogs => PlayniteApi.Dialogs;
        private IItemCollection<Tag> Tags => PlayniteApi.Database.Tags;
    }
}