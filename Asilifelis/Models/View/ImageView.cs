namespace Asilifelis.Models.View;

public record ImageView(Uri Url, bool Sensitive = false, string? Name = null, string Type = "Image");