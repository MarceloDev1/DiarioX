namespace DiarioX.Server.Application.Interfaces;

public interface IAppUrlProvider
{
    /// <summary>
    /// URL pública do front-end para a instituição da requisição atual
    /// (ex.: https://colegio-x.diariox.online) ou a URL global quando não há instituição.
    /// Não consulta o banco, então pode ser usada em envios de e-mail em segundo plano.
    /// </summary>
    string GetAppUrl();
}
