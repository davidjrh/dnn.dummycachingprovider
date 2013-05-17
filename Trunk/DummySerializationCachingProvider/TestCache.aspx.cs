
using System;
using System.Web;
using System.Web.UI;

#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Web.UI;

using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Modules.Actions;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Security;
using DotNetNuke.Security.Permissions;
using DotNetNuke.Services.Localization;
using DotNetNuke.UI.Containers;
using DotNetNuke.UI.Modules;
using DotNetNuke.UI.WebControls;

#endregion

public partial class TestCache: Page {
        override protected void OnInit(EventArgs e)
        {
            InitializeComponent();
            base.OnInit(e);
        }

        private void InitializeComponent()
        {
            Load += PageLoad;            
        }

	public string Message;
	public string CacheContent;


        private void PageLoad(object sender, EventArgs e)
        {
            try
            {
                if (!String.IsNullOrEmpty(Request.QueryString["clear"]))
                {
                    foreach (DictionaryEntry cacheItem in HttpRuntime.Cache)
                    {
                        HttpRuntime.Cache.Remove(cacheItem.Key.ToString());
                    }
                }

		        foreach (DictionaryEntry cacheItem in HttpRuntime.Cache)
		        {			
				        if (cacheItem.Key.ToString().StartsWith("SERIALIZATION_ERROR_"))
					        CacheContent += String.Format("<tr><td style='background-color: red; color: white'>{0}</td><td>{1}</td></tr>", cacheItem.Key,  cacheItem.Value);
				        else
					        CacheContent += String.Format("<tr><td>{0}</td><td>{1}</td></tr>", cacheItem.Key, cacheItem.Value);		
                }

            }
            catch (Exception ex)
            {
                        // Ignore errors, there's not much we can really do about it.
		        Response.Write(string.Format("<pre style='background-color: red; color: white; font-size: 10pt;'>{0}</pre>", ex));
            }
        }

}