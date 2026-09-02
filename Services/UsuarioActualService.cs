using Blazored.LocalStorage;

namespace ProyectoBlazor.Services;

public class UsuarioActualService
{
    private readonly ILocalStorageService _localStorage;

    private const string UsuarioIdKey = "usuarioId";

    public int UsuarioId { get; private set; }

    public UsuarioActualService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task EstablecerUsuarioAsync(int usuarioId) //Operación asincrónica
    {
        UsuarioId = usuarioId;

        await _localStorage.SetItemAsync(
            UsuarioIdKey,
            usuarioId);
    }

    public async Task<bool> CargarUsuarioAsync()
    {
        if (!await _localStorage.ContainKeyAsync(UsuarioIdKey))
        {
            UsuarioId = 0;
            return false;

        }
        
        UsuarioId = await _localStorage.GetItemAsync<int>(
            UsuarioIdKey);
        if (UsuarioId <= 0)
        {
            UsuarioId = 0;
            return false;
        }

        return UsuarioId > 0;
    }

    public async Task CerrarUsuarioAsync()
    {
        UsuarioId = 0;

        await _localStorage.RemoveItemAsync(
            UsuarioIdKey);
    }

    public bool HayUsuarioSeleccionado()
    {
        return UsuarioId > 0;
    }
}

//Por ahora su única responsabilidad es almacenar en el localsotrage que brinda Blazor la información
// de qué usuario está utilizando StudyFlow.