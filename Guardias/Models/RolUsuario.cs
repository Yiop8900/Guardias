namespace Guardias.Models;

public enum RolUsuario
{
    SuperAdmin = 0,
    Admin = 1,
    JefeOperaciones = 2,
    Mayordomo = 3,
    Conserje = 4,
    Guardia = 5
}

public static class RolUsuarioExtensions
{
    public static string Nombre(this RolUsuario rol) => rol switch
    {
        RolUsuario.SuperAdmin => "Super Admin",
        RolUsuario.Admin => "Administrador",
        RolUsuario.JefeOperaciones => "Jefe de Operaciones",
        RolUsuario.Mayordomo => "Mayordomo",
        RolUsuario.Conserje => "Conserje",
        _ => "Guardia"
    };

    public static bool TieneAccesoAdmin(this RolUsuario rol) =>
        rol is RolUsuario.SuperAdmin or RolUsuario.Admin or RolUsuario.JefeOperaciones;
}
