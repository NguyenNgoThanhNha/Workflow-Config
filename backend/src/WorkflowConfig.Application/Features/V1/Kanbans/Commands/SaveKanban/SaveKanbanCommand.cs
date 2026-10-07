using System.Text.Json.Serialization;
using FluentValidation;
using WorkflowConfig.Application.Features.V1.Kanbans.DTOs;
using WorkflowConfig.Application.Features.V1.Kanbans.Queries.GetKanban;
using WorkflowConfig.Domain.Entities.Workflows;

namespace WorkflowConfig.Application.Features.V1.Kanbans.Commands.SaveKanban;

/// <summary>Một cột trên form. Id = null → cột mới.</summary>
public sealed record KanbanColumnInput(Guid? Id, string Name, int OrderIndex, string? Note, string? Color);

/// <summary>Tạo (Id = null) hoặc sửa bảng Kanban cùng danh sách cột. Cột bị bỏ khỏi form → xóa cùng các trạng thái đã xếp vào cột đó.</summary>
public sealed record SaveKanbanCommand(
    string Code,
    string Name,
    int? OrderIndex,
    bool? IsActive,
    IReadOnlyList<KanbanColumnInput> Columns,
    byte[]? RowVersion) : IRequest<KanbanDetailDto>
{
    [JsonIgnore]
    public Guid? Id { get; init; }
}

public sealed class SaveKanbanCommandValidator : AbstractValidator<SaveKanbanCommand>
{
    private const string ColorPattern = "^#[0-9A-Fa-f]{6}$";

    public SaveKanbanCommandValidator(IUnitOfWork<WorkflowConfigDbContext> unitOfWork)
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100)
            .MustAsync(async (cmd, code, ct) => !await unitOfWork.Repository<Kanban>()
                .AnyAsync(k => k.Code == code.Trim() && k.Id != (cmd.Id ?? Guid.Empty), ct))
            .WithMessage("Mã Kanban đã tồn tại.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(250);
        RuleFor(x => x.OrderIndex).NotNull().GreaterThanOrEqualTo(0);
        RuleFor(x => x.RowVersion).NotEmpty().When(x => x.Id is not null).WithMessage("Thiếu rowVersion khi sửa.");
        RuleFor(x => x.Columns).NotEmpty().WithMessage("Kanban phải có ít nhất một cột.");
        RuleForEach(x => x.Columns).ChildRules(c =>
        {
            c.RuleFor(x => x.Name).NotEmpty().WithMessage("Nhập tên cột.").MaximumLength(250);
            c.RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(0);
            c.RuleFor(x => x.Note).MaximumLength(1000);
            c.RuleFor(x => x.Color).Matches(ColorPattern).When(x => !string.IsNullOrEmpty(x.Color)).WithMessage("Màu dạng #RRGGBB.");
        });
    }
}

public sealed class SaveKanbanCommandHandler(IUnitOfWork<WorkflowConfigDbContext> unitOfWork) : IRequestHandler<SaveKanbanCommand, KanbanDetailDto>
{
    public async Task<KanbanDetailDto> Handle(SaveKanbanCommand request, CancellationToken ct)
    {
        var kanbans = unitOfWork.Repository<Kanban>();
        Kanban kanban;
        List<KanbanColumn> columns = [];
        if (request.Id is { } id)
        {
            kanban = await kanbans.FirstOrDefaultAsync(k => k.Id == id, ct) ?? throw new NotFoundException("Kanban", id);
            kanbans.Entry(kanban).Property(k => k.RowVersion).OriginalValue = request.RowVersion!;
            columns = await unitOfWork.Repository<KanbanColumn>().Where(c => c.KanbanId == id).ToListAsync(ct);
        }
        else
        {
            kanban = new Kanban { Code = request.Code, Name = request.Name };
            kanbans.Add(kanban);
        }

        kanban.Code = request.Code.Trim();
        kanban.Name = request.Name.Trim();
        kanban.OrderIndex = request.OrderIndex!.Value;
        kanban.IsActive = request.IsActive ?? true;
        kanban.RefreshSearchText();

        // Cột bị bỏ → xóa luôn các trạng thái đã xếp vào cột đó (trạng thái quay về "Chưa cấu hình").
        var keptIds = request.Columns.Where(c => c.Id is not null).Select(c => c.Id!.Value).ToHashSet();
        var removedIds = columns.Where(c => !keptIds.Contains(c.Id)).Select(c => c.Id).ToList();
        if (removedIds.Count > 0)
        {
            unitOfWork.Repository<KanbanStatusMapping>().RemoveRange(
                await unitOfWork.Repository<KanbanStatusMapping>().Where(m => removedIds.Contains(m.ColumnId)).ToListAsync(ct));
        }

        var byId = columns.ToDictionary(c => c.Id);
        foreach (var input in request.Columns)
        {
            if (input.Id is not { } columnId || !byId.TryGetValue(columnId, out var column))
            {
                column = new KanbanColumn { KanbanId = kanban.Id, Name = input.Name };
                unitOfWork.Repository<KanbanColumn>().Add(column);
            }
            column.Name = input.Name.Trim();
            column.OrderIndex = input.OrderIndex;
            column.Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim();
            column.Color = string.IsNullOrWhiteSpace(input.Color) ? null : input.Color.Trim().ToUpperInvariant();
        }
        unitOfWork.Repository<KanbanColumn>().RemoveRange(columns.Where(c => removedIds.Contains(c.Id)));

        // Sửa cột cũng là sửa bảng: UPDATE dòng cha để tăng RowVersion.
        if (request.Id is not null) kanbans.Entry(kanban).State = EntityState.Modified;

        await unitOfWork.SaveChangesAsync(ct);
        return await KanbanDetailReader.ReadAsync(unitOfWork, kanban.Id, ct);
    }
}
