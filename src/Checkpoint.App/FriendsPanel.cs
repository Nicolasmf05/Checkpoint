using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Checkpoint.Core;

namespace Checkpoint.App;

internal sealed class FriendsPanel : ScrollViewer
{
    private readonly MainWindow owner;
    private readonly StackPanel body = new() { Margin = new Thickness(0,0,5,5) };
    private SocialProfile[] profiles = [];
    private SocialRequest[] requests = [];
    private HashSet<Guid> friends = [];
    private Guid[] blocks = [];
    private string page = I18n.T("Amigos");
    private Guid? selectedFriend;
    private SocialPublication[] publications = [];
    private SocialPublication[] ownPublications = [];
    private int generation;
    private string message = "";
    public bool Busy { get; private set; }
    internal void RefreshLanguage() { page = I18n.T("Amigos"); message = ""; selectedFriend = null; Render(); }
    internal FriendsPanel(MainWindow owner)
    {
        this.owner = owner; Content = body; VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled; Render();
    }
    private TextBlock Text(string text, double size = 12, bool muted = false) => new()
    {
        Text = text, FontSize = size, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,0,0,8),
        Foreground = (Brush)Application.Current.Resources[muted ? "MutedBrush" : "TextBrush"]
    };
    private Button Action(string text, Func<Task> action)
    {
        var button = new Button { Content = text, Padding = new Thickness(9,6,9,6), Margin = new Thickness(0,0,5,6), IsEnabled = !Busy };
        System.Windows.Automation.AutomationProperties.SetName(button,text);
        button.Click += async (_, _) => await Run(action); return button;
    }
    private async Task Run(Func<Task> action)
    {
        if (Busy) return;
        Busy = true;
        try { await action(); }
        catch (Exception ex) when (ex is System.Net.Http.HttpRequestException or InvalidOperationException or IOException or ArgumentException or OperationCanceledException or System.Security.Cryptography.CryptographicException)
        { message = ex is System.Net.Http.HttpRequestException ? I18n.T("Sin conexión. Puedes seguir usando tu biblioteca local.") : ex is System.Security.Cryptography.CryptographicException ? I18n.T("Windows no pudo guardar la sesión de forma segura. Vuelve a abrir Checkpoint con tu usuario habitual.") : I18n.Error(ex); }
        finally { Busy = false; Render(); owner.Refresh(); }
    }
    public async Task Reload()
    {
        if (Busy) return;
        await Run(Load);
    }
    private async Task Load()
    {
        if (owner.Social?.Session is not { } session) { ClearRemote(); return; }
        var api = owner.Social;
        try
        {
            var profileTask = api.Profiles(); var requestTask = api.Requests(); var friendTask = api.Friendships(); var blockTask = api.Blocks();
            var ownTask = api.Publications(session.UserId);
            await Task.WhenAll(profileTask,requestTask,friendTask,blockTask,ownTask);
            profiles = await profileTask; requests = await requestTask;
            ownPublications = await ownTask;
            friends = (await friendTask).Select(f => f.UserLow == session.UserId ? f.UserHigh : f.UserLow).ToHashSet();
            blocks = (await blockTask).Select(b => b.GetProperty("blocked_id").GetGuid()).ToArray();
            if (selectedFriend is Guid target && friends.Contains(target)) publications = (await api.Publications(target)).Where(p => p.IsShared && p.Payload is not null).ToArray();
            else { selectedFriend = null; publications = []; }
        }
        catch { ClearRemote(); throw; }
    }
    private void ClearRemote() { profiles = []; requests = []; friends = []; blocks = []; publications = []; ownPublications = []; selectedFriend = null; generation++; }
    private void Render()
    {
        generation++; body.Children.Clear();
        if (owner.Social is null) { body.Children.Add(Text(I18n.T("El servicio de cuentas no está configurado en esta edición."))); return; }
        if (message.Length > 0) body.Children.Add(Text(message,11,true));
        if (owner.Social.Session is null) { RenderLogin(); return; }
        var navigation = new WrapPanel(); body.Children.Add(navigation);
        foreach (string tab in new[] { I18n.T("Amigos"), I18n.T("Solicitudes"), I18n.T("Compartir"), I18n.T("Cuenta") })
            navigation.Children.Add(Action(tab,async () => { page = tab; selectedFriend = null; message = ""; await Load(); }));
        if (page == I18n.T("Compartir")) RenderSharing();
        else if (page == I18n.T("Cuenta")) RenderAccount();
        else if (page == I18n.T("Solicitudes")) RenderRequests();
        else RenderFriends();
    }
    private TextBox Input(StackPanel panel, string label, int maxLength = 254)
    {
        panel.Children.Add(Text(label,11,true));
        var box = new TextBox { MaxLength = maxLength, Margin = new Thickness(0,0,0,10) };
        System.Windows.Automation.AutomationProperties.SetName(box,label); panel.Children.Add(box); return box;
    }
    private void RenderLogin()
    {
        body.Children.Add(Text(I18n.T("Una cuenta de Checkpoint"),18));
        body.Children.Add(Text(I18n.T("Añade a tus amigos y comparte solo los juegos que elijas. Tu biblioteca sigue disponible sin cuenta."),12,true));
        var username = Input(body,I18n.T("Usuario de Checkpoint"),24);
        body.Children.Add(Text(I18n.T("3–24 letras, números o guiones bajos. El usuario no distingue mayúsculas."),10,true));
        body.Children.Add(Text(I18n.T("Contraseña de Checkpoint"),11,true));
        var password = new PasswordBox { MaxLength = 200, Margin = new Thickness(0,0,0,12), Padding = new Thickness(10,7,10,7),
            Background = (Brush)Application.Current.Resources["InputBrush"], Foreground = (Brush)Application.Current.Resources["TextBrush"] };
        System.Windows.Automation.AutomationProperties.SetName(password,I18n.T("Contraseña de Checkpoint")); body.Children.Add(password);
        var name = Input(body,I18n.T("Nombre visible (opcional al crear cuenta)"),50);
        var actions = new WrapPanel(); body.Children.Add(actions);
        actions.Children.Add(Action(I18n.T("Entrar"),async () => { await owner.Social!.Login(username.Text,password.Password); password.Clear(); owner.SchedulePublications(); message = I18n.T("Sesión iniciada."); await Load(); await owner.PublishSocial(); }));
        actions.Children.Add(Action(I18n.T("Crear cuenta"),async () => {
            bool entered = await owner.Social!.Register(username.Text,password.Password,string.IsNullOrWhiteSpace(name.Text) ? username.Text.Trim() : name.Text); password.Clear();
            message = entered ? I18n.T("Cuenta creada. Ya puedes añadir amigos.") : I18n.T("El servicio todavía tiene activada la confirmación. El registro por usuario debe habilitarse en la configuración del proyecto.");
            if (entered) { owner.SchedulePublications(); await Load(); }
        }));
        body.Children.Add(Text(I18n.T("Guarda tu contraseña. Esta edición no recupera el acceso por correo."),11,true));
    }
    private string PlayerName(Guid id) => profiles.FirstOrDefault(p => p.UserId == id)?.DisplayName ?? I18n.T("Jugador de Checkpoint");
    private StackPanel Card(string title, string? detail = null)
    {
        var panel = new StackPanel(); panel.Children.Add(Text(title,14));
        if (detail is not null) panel.Children.Add(Text(detail,11,true));
        body.Children.Add(new Border { Child = panel, Background = (Brush)Application.Current.Resources["PanelBrush"],
            BorderBrush = (Brush)Application.Current.Resources["LineBrush"], BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12), Margin = new Thickness(0,7,0,5) }); return panel;
    }
    private void RenderFriends()
    {
        if (selectedFriend is Guid target)
        {
            body.Children.Add(Action(I18n.T("← Volver a mis amigos"),() => { selectedFriend = null; publications = []; return Task.CompletedTask; }));
            body.Children.Add(Text(PlayerName(target),18));
            body.Children.Add(Text((I18n.IsEnglish ? $"{publications.Length} shared games · published from their Checkpoint" : $"{publications.Length} juegos compartidos · datos publicados desde su Checkpoint"),11,true));
            if (publications.Length == 0) body.Children.Add(Text(I18n.T("Tu amigo aún no ha compartido juegos.")));
            foreach (var publication in publications.OrderByDescending(p => p.UpdatedAt).Take(200)) RenderPublication(publication);
            if (publications.Length > 200) body.Children.Add(Text(I18n.T("Se muestran las 200 publicaciones más recientes."),11,true));
            body.Children.Add(Action(I18n.T("Actualizar progreso"),Load)); return;
        }
        body.Children.Add(Text((I18n.IsEnglish ? $"Your friends · {friends.Count}" : $"Tus amigos · {friends.Count}"),18));
        var code = Input(body,I18n.T("Código de amigo (checkpoint-…)"),FriendCodes.DisplayLength);
        body.Children.Add(Action(I18n.T("Enviar solicitud"),async () => {
            var found = await owner.Social!.Find(code.Text);
            if (found.Length == 0) throw new InvalidOperationException(I18n.T("No se encontró un usuario disponible con ese código."));
            await owner.Social.Invite(found[0].UserId); message = I18n.T("Solicitud enviada a ") + found[0].DisplayName + "."; await Load();
        }));
        if (friends.Count == 0) body.Children.Add(Text(I18n.T("Comparte tu código desde Cuenta. La amistad empieza cuando se acepta la solicitud."),12,true));
        foreach (var friend in friends.OrderBy(PlayerName))
        {
            var card = Card(PlayerName(friend)); var actions = new WrapPanel(); card.Children.Add(actions);
            actions.Children.Add(Action(I18n.T("Ver progreso de ") + PlayerName(friend),async () => { selectedFriend = friend; await Load(); }));
            actions.Children.Add(Action(I18n.T("Quitar amistad"),async () => { await owner.Social!.Unfriend(friend); message = I18n.T("Amistad retirada."); await Load(); }));
            actions.Children.Add(Action(I18n.T("Bloquear"),async () => { await owner.Social!.Block(friend); message = I18n.T("Usuario bloqueado. Ya no puede ver tus publicaciones."); await Load(); }));
        }
        body.Children.Add(Action(I18n.T("Actualizar amigos"),Load));
    }
    private void RenderPublication(SocialPublication publication)
    {
        var game = publication.Payload!; var card = Card(game.Title,game.Platform + " · " + game.StatusText);
        var details = new StackPanel();
        foreach (var child in card.Children.Cast<UIElement>().ToArray()) { card.Children.Remove(child); details.Children.Add(child); }
        var image = new Image { Stretch = Stretch.UniformToFill };
        var cover = new Grid { Width = 70, Height = 103, Background = (Brush)Application.Current.Resources["InputBrush"] };
        cover.Visibility = owner.Preferences.LightweightMode ? Visibility.Collapsed : Visibility.Visible;
        cover.Children.Add(new TextBlock { Text = string.IsNullOrEmpty(game.Title) ? "?" : game.Title[..1].ToUpperInvariant(),
            FontSize = 32, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }); cover.Children.Add(image);
        var grid = new Grid(); grid.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); grid.ColumnDefinitions.Add(new());
        grid.Children.Add(cover); details.Margin = new Thickness(owner.Preferences.LightweightMode ? 0 : 12,0,0,0); Grid.SetColumn(details,1); grid.Children.Add(details); card.Children.Add(grid);
        int current = generation; if (!owner.Preferences.LightweightMode) _ = LoadCover(image,publication,current);
        details.Children.Add(Text(game.GoalKind == "custom" ? game.GoalText ?? I18n.T("Objetivo personal") : game.GoalKind == "story" ? I18n.T("Objetivo: terminar la historia") : I18n.T("Objetivo: todos los logros"),11,true));
        if (game.ProgressText.Length > 0) details.Children.Add(Text(game.ProgressText,12));
        details.Children.Add(Text(I18n.T("Publicado ") + publication.UpdatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),10,true));
    }
    private async Task LoadCover(Image image, SocialPublication publication, int current)
    {
        try
        {
            var game = publication.Payload!; BitmapImage? bitmap = null;
            if (game.CoverPath is string path)
            {
                var bytes = await owner.Social!.DownloadCover(path,publication.OwnerId);
                BackupFiles.ValidatePngImage(bytes);
                using var input = new MemoryStream(bytes); bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 220; bitmap.StreamSource = input; bitmap.EndInit(); bitmap.Freeze();
            }
            else if (game.SteamAppId is int app) bitmap = await owner.Covers.Get(new Game { Title = game.Title, SteamAppId = app });
            if (current == generation) image.Source = bitmap;
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or System.Net.Http.HttpRequestException or ArgumentException or NotSupportedException or FormatException or OperationCanceledException) { }
    }
    private void RenderRequests()
    {
        body.Children.Add(Text(I18n.T("Solicitudes"),18));
        if (requests.Length == 0) body.Children.Add(Text(I18n.T("No hay solicitudes pendientes."),12,true));
        foreach (var request in requests)
        {
            bool received = request.RecipientId == owner.Social!.Session!.UserId;
            var card = Card(PlayerName(received ? request.SenderId : request.RecipientId),received ? I18n.T("Quiere añadirte como amigo") : I18n.T("Esperando aceptación"));
            var actions = new WrapPanel(); card.Children.Add(actions);
            if (received)
            {
                actions.Children.Add(Action(I18n.T("Aceptar"),async () => { await owner.Social.Answer(request.Id,true); message = I18n.T("Amistad aceptada."); await Load(); }));
                actions.Children.Add(Action(I18n.T("Rechazar"),async () => { await owner.Social.Answer(request.Id,false); message = I18n.T("Solicitud rechazada."); await Load(); }));
            }
            else actions.Children.Add(Action(I18n.T("Cancelar solicitud"),async () => { await owner.Social.Cancel(request.Id); await Load(); }));
        }
    }
    private void RenderSharing()
    {
        body.Children.Add(Text(I18n.T("Elige qué compartir"),18));
        body.Children.Add(Text(I18n.T("Tus amigos verán título, carátula, estado, objetivo y contadores de progreso. Las notas y los nombres de las tareas son privados."),11,true));
        var pending = owner.Outbox?.Games.Values.Count(e => e.HasWork) ?? 0;
        body.Children.Add(Text(pending > 0 ? (I18n.IsEnglish ? $"{pending} pending publications. They retry when connected." : $"{pending} publicaciones pendientes. Se reintentan con conexión.") : I18n.T("No hay publicaciones pendientes."),11,true));
        body.Children.Add(Action(I18n.T("Publicar cambios ahora"),async () => { await owner.PublishSocial(); await Load(); message = I18n.T("Cola de publicación revisada."); }));
        var search = Input(body,I18n.T("Buscar juegos para compartir")); var rows = new StackPanel(); body.Children.Add(rows);
        void Fill()
        {
            rows.Children.Clear();
            var matching = GameRules.InDisplayOrder(owner.Games.Where(g => g.Title.Contains(search.Text,StringComparison.CurrentCultureIgnoreCase))).ToArray();
            foreach (var game in matching.Take(200))
            {
                var entry = owner.Outbox?.Games.GetValueOrDefault(game.Id);
                var row = new StackPanel { Margin = new Thickness(0,8,0,8) };
                var check = new CheckBox { Content = new TextBlock { Text = game.Title, TextWrapping = TextWrapping.Wrap }, IsChecked = entry?.Selected == true };
                System.Windows.Automation.AutomationProperties.SetName(check,I18n.T("Compartir ") + game.Title);
                check.Click += (_, _) => {
                    try { owner.SetShared(game,check.IsChecked == true); message = I18n.T("Cambio guardado. Se publicará cuando haya conexión."); }
                    catch (Exception ex) when (ex is IOException or ArgumentException or InvalidOperationException) { check.IsChecked = entry?.Selected == true; message = I18n.Error(ex); }
                    owner.Notice(message);
                };
                row.Children.Add(check);
                bool remoteShared = ownPublications.Any(p => p.GameId == game.Id && p.IsShared);
                row.Children.Add(Text(entry?.Conflict == true ? I18n.T("Conflicto con otra publicación") : entry?.HasWork == true ? I18n.T("Pendiente de publicar") : entry?.Selected == true ? I18n.T("Compartido con tus amigos") : remoteShared ? I18n.T("Publicado desde otro PC") : I18n.T("Privado"),10,true));
                if (entry?.Conflict == true) row.Children.Add(Action(I18n.T("Publicar mi versión de ") + game.Title,async () => {
                    owner.Outbox!.ResolveWithLocal(game.Id); owner.SaveOutbox(); await owner.PublishSocial();
                }));
                rows.Children.Add(row);
            }
            if (matching.Length > 200) rows.Children.Add(Text(I18n.T("Se muestran 200 juegos. Usa la búsqueda para encontrar el resto."),11,true));
        }
        search.TextChanged += (_, _) => Fill(); Fill();
        foreach (var remote in ownPublications.Where(p => p.IsShared && owner.Outbox?.Games.GetValueOrDefault(p.GameId)?.Selected != true))
        {
            var card = Card(remote.Payload?.Title ?? I18n.T("Publicación de otro PC"),I18n.T("Publicado en tu cuenta; puedes retirarlo desde aquí."));
            card.Children.Add(Action(I18n.T("Retirar publicación de ") + (remote.Payload?.Title ?? remote.GameId.ToString()),async () => {
                if (owner.SocialSyncing) throw new InvalidOperationException(I18n.T("Espera a que termine la publicación en curso."));
                owner.Outbox!.Reconcile(remote.GameId,remote); owner.Outbox.SetDesired(remote.GameId,null);
                owner.SaveOutbox(); await owner.PublishSocial(); await Load();
            }));
        }
        foreach (var missing in owner.Outbox?.Games.Where(g => g.Value.HasWork && owner.Games.All(local => local.Id != g.Key)) ?? [])
            body.Children.Add(Text(I18n.T("Retirada pendiente de un juego eliminado."),11,true));
    }
    private void RenderAccount()
    {
        var profile = profiles.FirstOrDefault(p => p.UserId == owner.Social!.Session!.UserId);
        body.Children.Add(Text(I18n.T("Tu cuenta de Checkpoint"),18));
        if (profile is not null)
        {
            body.Children.Add(Text(FriendCodes.Display(profile.FriendCode),18));
            body.Children.Add(Action(I18n.T("Copiar mi código"),() => { Clipboard.SetText(FriendCodes.Display(profile.FriendCode)); message = I18n.T("Código copiado."); return Task.CompletedTask; }));
            var name = Input(body,I18n.T("Tu nombre en Checkpoint"),50); name.Text = profile.DisplayName;
            body.Children.Add(Action(I18n.T("Guardar nombre"),async () => { await owner.Social!.UpdateName(name.Text); await Load(); message = I18n.T("Nombre guardado."); }));
        }
        if (blocks.Length > 0) body.Children.Add(Text(I18n.T("Usuarios bloqueados"),14));
        foreach (var blocked in blocks) body.Children.Add(Action(I18n.T("Desbloquear ") + blocked.ToString()[..8],async () => {
            await owner.Social!.Unblock(blocked); await Load(); message = I18n.T("Desbloqueado. Para ser amigos hace falta una nueva aceptación.");
        }));
        body.Children.Add(Text(I18n.T("Cerrar sesión conserva tus publicaciones y cambios pendientes para esta cuenta. Para retirarlas, desmarca los juegos en Compartir y espera a que se publiquen los cambios."),11,true));
        body.Children.Add(Action(I18n.T("Cerrar sesión de Checkpoint"),async () => {
            if (owner.SocialSyncing) throw new InvalidOperationException(I18n.T("Espera a que termine la publicación antes de cerrar sesión."));
            try { await owner.Social!.Logout(); } finally { ClearRemote(); }
            message = I18n.T("Sesión cerrada en este PC.");
        }));
    }
}
