<%@ Page Language="C#" AutoEventWireup="false"  Inherits="TestCache" CodeFile="TestCache.aspx.cs" %>
<html>
<style>
BODY, TD {
	font-family: Segoe UI, Arial; 
	font-size: 0.8em;
}
th { text-align: left; background-color: #f0f0f0; }
.message { background-color: #f0f0f0; border: 1px solid #ccc; font-weight: bold; padding: 10px; }
</style>
<body>
<h1>Dummy Serialization Caching Provider: serialization test</h1>

<h2>Dummy Serialization Caching Provider contents</h2>
<p>The following objects were successfully cached by the DotNetNuke Dummy Serialization Caching provider. When a serialization error occurs, the cache key is inserted by using the format <b>SERIALIZATION_ERROR_ + [Original cache key]</b>.</p>
<table>
<tr><th>Cache Key</th><th>Value</th></tr>
<% = CacheContent %>
</table>
</body>
</html>