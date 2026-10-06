using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioX.Server.Infrastructure.Data.Configurations;

public class CalendarioLetivoConfiguration : IEntityTypeConfiguration<CalendarioLetivo>
{
    public void Configure(EntityTypeBuilder<CalendarioLetivo> builder)
    {
        builder.ToTable("calendarios_letivos");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(x => x.AnoLetivoId).HasColumnName("ano_letivo_id").IsRequired();
        builder.Property(x => x.EscolaId).HasColumnName("escola_id");
        builder.Property(x => x.PublicadoEm).HasColumnName("publicado_em");
        builder.Property(x => x.PublicadoPorUsuarioId).HasColumnName("publicado_por_usuario_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.Ignore(x => x.Publicado);

        builder.HasOne(x => x.AnoLetivo).WithMany().HasForeignKey(x => x.AnoLetivoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Escola).WithMany().HasForeignKey(x => x.EscolaId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.PublicadoPorUsuarioId).OnDelete(DeleteBehavior.Restrict);

        // Um calendário da rede e um por escola em cada ano letivo (escola_id nulo não colide no índice único).
        builder.HasIndex(x => new { x.AnoLetivoId, x.EscolaId })
            .HasDatabaseName("IX_calendarios_letivos_ano_escola")
            .HasFilter("escola_id IS NOT NULL")
            .IsUnique();
        builder.HasIndex(x => x.AnoLetivoId)
            .HasDatabaseName("IX_calendarios_letivos_ano_rede")
            .HasFilter("escola_id IS NULL")
            .IsUnique();
    }
}

public class EventoCalendarioConfiguration : IEntityTypeConfiguration<EventoCalendario>
{
    public void Configure(EntityTypeBuilder<EventoCalendario> builder)
    {
        builder.ToTable("eventos_calendario", table =>
            table.HasCheckConstraint("CK_eventos_calendario_tipo",
                $"tipo IN ({string.Join(", ", EventoCalendario.Tipos.Keys.Select(t => $"'{t}'"))})"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(x => x.CalendarioLetivoId).HasColumnName("calendario_letivo_id").IsRequired();
        builder.Property(x => x.Data).HasColumnName("data").HasColumnType("date").IsRequired();
        builder.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(EventoCalendario.MaxDescricao).IsRequired();
        builder.Property(x => x.ComAula).HasColumnName("com_aula").IsRequired();

        builder.HasOne(x => x.CalendarioLetivo)
            .WithMany(c => c.Eventos)
            .HasForeignKey(x => x.CalendarioLetivoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Um evento por dia em cada calendário: cadastrar outro no mesmo dia substitui o anterior.
        builder.HasIndex(x => new { x.CalendarioLetivoId, x.Data })
            .HasDatabaseName("IX_eventos_calendario_calendario_data")
            .IsUnique();
    }
}
