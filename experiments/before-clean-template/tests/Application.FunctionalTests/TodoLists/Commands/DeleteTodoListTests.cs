using LayerCake.CleanTemplate.Application.TodoLists.Commands.CreateTodoList;
using LayerCake.CleanTemplate.Application.TodoLists.Commands.DeleteTodoList;
using LayerCake.CleanTemplate.Domain.Entities;

namespace LayerCake.CleanTemplate.Application.FunctionalTests.TodoLists.Commands;

public class DeleteTodoListTests : TestBase
{
    [Test]
    public async Task ShouldRequireValidTodoListId()
    {
        var command = new DeleteTodoListCommand(99);
        await Should.ThrowAsync<NotFoundException>(() => TestApp.SendAsync(command));
    }

    [Test]
    public async Task ShouldDeleteTodoList()
    {
        var listId = await TestApp.SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });

        await TestApp.SendAsync(new DeleteTodoListCommand(listId));

        var list = await TestApp.FindAsync<TodoList>(listId);

        list.ShouldBeNull();
    }
}
