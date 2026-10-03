using System.Text.Json;
using System.Text.RegularExpressions;
namespace Checkpoint.Core;
public sealed record AppUpdate(Version Version,string Tag,string Name,Uri Installer,Uri Checksum,long Size,string Digest,bool Preview)
{
    public string ReleaseUrl=>"https://github.com/Nicolasmf05/Checkpoint/releases/tag/"+Tag;
}
public static class AppUpdates
{
    public const string Releases="https://api.github.com/repos/Nicolasmf05/Checkpoint/releases?per_page=100";
    public static AppUpdate? Select(string json,Version current,string runtime)
    {
        if(runtime is not ("win-x64" or "win-arm64"))throw new ArgumentException(nameof(runtime));
        using var document=JsonDocument.Parse(json);if(document.RootElement.ValueKind!=JsonValueKind.Array)throw new InvalidDataException();
        var found=new List<AppUpdate>();
        foreach(var release in document.RootElement.EnumerateArray().Take(100))
        {
            if(release.ValueKind!=JsonValueKind.Object||!release.TryGetProperty("draft",out var draft)||draft.ValueKind!=JsonValueKind.False||!release.TryGetProperty("tag_name",out var tagValue))continue;
            string tag=tagValue.ValueKind==JsonValueKind.String?tagValue.GetString()??"":"";
            if(!Regex.IsMatch(tag,@"^v[0-9]{1,5}\.[0-9]{1,5}\.[0-9]{1,5}$")||!Version.TryParse(tag[1..],out var version)||version<=current||!release.TryGetProperty("assets",out var assets)||assets.ValueKind!=JsonValueKind.Array)continue;
            string name=$"Checkpoint-{version}-{runtime}.msi";var rows=assets.EnumerateArray().Where(a=>a.ValueKind==JsonValueKind.Object).ToArray();
            JsonElement? Find(string file)=>rows.Cast<JsonElement?>().FirstOrDefault(a=>a!.Value.TryGetProperty("name",out var n)&&n.ValueKind==JsonValueKind.String&&n.GetString()==file);
            bool Valid(JsonElement asset,string file,long maximum)=>asset.TryGetProperty("browser_download_url",out var u)&&u.ValueKind==JsonValueKind.String&&u.GetString()==$"https://github.com/Nicolasmf05/Checkpoint/releases/download/{tag}/{file}"&&asset.TryGetProperty("size",out var s)&&s.TryGetInt64(out long size)&&size>0&&size<=maximum&&asset.TryGetProperty("state",out var state)&&state.GetString()=="uploaded";
            var installer=Find(name);var checksum=Find(name+".sha256");
            if(installer is not {} msi||checksum is not {} sha||!Valid(msi,name,500_000_000)||!Valid(sha,name+".sha256",4096)||!msi.TryGetProperty("digest",out var digestValue)||digestValue.ValueKind!=JsonValueKind.String)continue;
            string digest=digestValue.GetString()??"";if(!Regex.IsMatch(digest,@"^sha256:[a-fA-F0-9]{64}$"))continue;
            found.Add(new(version,tag,name,new(msi.GetProperty("browser_download_url").GetString()!),new(sha.GetProperty("browser_download_url").GetString()!),msi.GetProperty("size").GetInt64(),digest[7..].ToLowerInvariant(),release.TryGetProperty("prerelease",out var preview)&&preview.ValueKind==JsonValueKind.True));
        }
        return found.OrderByDescending(u=>u.Version).FirstOrDefault();
    }
    public static void Validate(AppUpdate update)
    {
        if(update.Tag!="v"+update.Version||!Regex.IsMatch(update.Name,@"^Checkpoint-"+Regex.Escape(update.Version.ToString())+@"-win-(x64|arm64)\.msi$")||update.Size<=0||update.Size>500_000_000||!Regex.IsMatch(update.Digest,@"^[a-fA-F0-9]{64}$")||update.Installer.AbsoluteUri!=$"https://github.com/Nicolasmf05/Checkpoint/releases/download/{update.Tag}/{update.Name}"||update.Checksum.AbsoluteUri!=update.Installer.AbsoluteUri+".sha256")
            throw new InvalidDataException(I18n.T("La dirección de actualización no es válida."));
    }
    public static string Checksum(string text,AppUpdate update)
    {
        var match=Regex.Match(text.Trim(),@"^([a-fA-F0-9]{64})[ \t]+\*?([^\r\n]+)$");
        if(!match.Success||match.Groups[2].Value!=update.Name||!match.Groups[1].Value.Equals(update.Digest,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException(I18n.T("La actualización no supera la comprobación de integridad. No se instalará."));
        return update.Digest;
    }
}
