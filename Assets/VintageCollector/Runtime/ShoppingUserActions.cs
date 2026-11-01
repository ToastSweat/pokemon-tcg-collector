using UnityEngine;using VintageCollector.Shopping;
namespace VintageCollector.Runtime {
 public interface IShoppingUserActions {void Search(WantedCard wanted,string provider);void CopyDescription(WantedCard wanted);void OpenListing(string provider,string validatedRawUrl);}
 // Only explicit host dispatch reaches these platform effects. No general URI-launch API.
 public sealed class ShoppingUserActions:IShoppingUserActions {
  public void Search(WantedCard wanted,string provider){Application.OpenURL(ShoppingLinks.Search(wanted,provider));}
  public void CopyDescription(WantedCard wanted){GUIUtility.systemCopyBuffer=ShoppingLinks.Query(wanted);}
  public void OpenListing(string provider,string validatedRawUrl){Application.OpenURL(ShoppingLinks.ValidateListing(validatedRawUrl,provider));}
 }
}
