using Pkmds.Rcl.Components.MainTabPages.Pokedex.Gen8La;

namespace Pkmds.Tests;

public class PokedexLaResearchPanelTests
{
    [Fact]
    public void CompleteSpeciesResearch_SkipsNonSettableTasksAndCompletesEditableTasks()
    {
        // Arrange
        var (saveFile, _, _, _) = BunitTestHelpers.LoadSave("legends-arceus");
        var sav = saveFile.Should().BeOfType<SAV8LA>().Subject;

        var speciesWithNonSettableTask = Enumerable.Range(1, sav.MaxSpeciesID)
            .Select(static species => (ushort)species)
            .Select(species =>
            {
                var dexIndex = PokedexSave8a.GetDexIndex(PokedexType8a.Hisui, species);
                var tasks = dexIndex == 0 ? [] : PokedexConstants8a.ResearchTasks[dexIndex - 1];
                return (Species: species, Tasks: tasks);
            })
            .First(x => x.Tasks.Any(task => !task.Task.CanSetCurrentValue()) &&
                        x.Tasks.Any(task => task.Task.CanSetCurrentValue() && task.TaskThresholds.Length > 0));

        var editableTaskIndex = Array.FindIndex(
            speciesWithNonSettableTask.Tasks,
            task => task.Task.CanSetCurrentValue() && task.TaskThresholds.Length > 0);
        var editableTask = speciesWithNonSettableTask.Tasks[editableTaskIndex];

        // Act
        var act = () => PokedexLaResearchPanel.CompleteSpeciesResearch(
            sav.PokedexSave,
            speciesWithNonSettableTask.Species);

        // Assert
        act.Should().NotThrow();
        sav.PokedexSave.GetResearchTaskLevel(
            speciesWithNonSettableTask.Species,
            editableTaskIndex,
            out _,
            out var currentValue,
            out _);
        currentValue.Should().Be(editableTask.TaskThresholds[^1]);
    }
}
