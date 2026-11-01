using System;
namespace VintageCollector.Shopping {
 [Serializable] public sealed class MoneyData { public bool priceKnown,shippingKnown,taxKnown,feesKnown;public long priceCents,shippingCents,taxCents,feesCents; }
 public sealed class MoneySummary { public long KnownSubtotalCents {get;internal set;}public bool FullTotalKnown {get;internal set;}public long FullTotalCents {get;internal set;} }
 public static class Money {
 public static bool TryParseUsd(string s,out long cents){cents=0;if(string.IsNullOrEmpty(s)||!System.Text.RegularExpressions.Regex.IsMatch(s,@"\A[0-9]+(?:\.[0-9]{1,2})?\z"))return false;try{string[] p=s.Split('.');long whole=long.Parse(p[0],System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture);long fraction=p.Length==1?0:long.Parse(p[1],System.Globalization.CultureInfo.InvariantCulture)*(p[1].Length==1?10:1);cents=checked(whole*100+fraction);return true;}catch(OverflowException){return false;}}
 public static MoneySummary Summarize(MoneyData d){if(d==null)throw new ArgumentNullException("d");long total=0;try{total=checked(Part(d.priceKnown,d.priceCents)+Part(d.shippingKnown,d.shippingCents)+Part(d.taxKnown,d.taxCents)+Part(d.feesKnown,d.feesCents));}catch(OverflowException){throw new ArgumentException("USD cents total overflow");}bool full=d.priceKnown&&d.shippingKnown&&d.taxKnown&&d.feesKnown;return new MoneySummary{KnownSubtotalCents=total,FullTotalKnown=full,FullTotalCents=full?total:0};}
 static long Part(bool known,long cents){if(cents<0||!known&&cents!=0)throw new ArgumentException("Invalid known/amount pair");return cents;}
 }
}
