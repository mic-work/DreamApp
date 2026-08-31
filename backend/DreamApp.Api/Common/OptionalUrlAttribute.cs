using System.ComponentModel.DataAnnotations;

namespace DreamApp.Api.Common;

/// <summary>
/// Like the built-in [Url], but treats a null/empty/whitespace value as
/// valid. The built-in [Url] attribute rejects an empty string (it only
/// special-cases null), so an OPTIONAL field like Gift.ProductUrl would
/// fail validation whenever the frontend submits "" for a field the user
/// simply left blank — even though the field is meant to be optional.
/// </summary>
public class OptionalUrlAttribute : ValidationAttribute
{
    public OptionalUrlAttribute()
        : base("Il link non è un URL valido.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is not string valueAsString || string.IsNullOrWhiteSpace(valueAsString))
        {
            return true;
        }

        return Uri.TryCreate(valueAsString, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
