using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace ProyectoBlazor.Services;

public class UsuarioActualService
{
    private readonly AuthenticationStateProvider _authStateProvider;

    public int UsuarioId { get; private set; }

    public string NombreUsuario { get; private set; } = string.Empty;

    public UsuarioActualService(
        AuthenticationStateProvider authStateProvider)
    {
        _authStateProvider = authStateProvider;
    }

    public async Task<bool> CargarUsuarioAsync()
    {
        var estado = await _authStateProvider
            .GetAuthenticationStateAsync();

        var id = estado.User.FindFirst(
            ClaimTypes.NameIdentifier)?.Value;

        UsuarioId = int.TryParse(id, out var valor)
            ? valor
            : 0;

        NombreUsuario = estado.User.Identity?.Name
            ?? string.Empty;

        return UsuarioId > 0;
    }

    public bool HayUsuarioSeleccionado()
    {
        return UsuarioId > 0;
    }
}
