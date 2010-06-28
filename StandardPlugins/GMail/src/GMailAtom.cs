//  
// Copyright (C) 2009 Robert Dyer
// Copyright (C) 2010 Robert Dyer
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Web;
using System.Xml;

using Gdk;
using Gtk;
using Cairo;
using Mono.Unix;

using Docky.Services;
using Docky.Widgets;

namespace GMail
{
	public enum GMailState
	{
		Normal,
		Reloading,
		ManualReload,
		Error
	}
	
	public struct UnreadMessage
	{
		public string Topic;
		public string From;
		public string FromName;
		public string FromEmail;
		public DateTime SendDate;
		public string Link;
	}
	
	/// <summary>
	/// </summary>
	public class GMailAtom
	{		
		static event EventHandler ResetNeeded;
		
		public static void SettingsChanged ()
		{
			if (ResetNeeded != null)
				ResetNeeded (null, new EventArgs());
		}
		
		public event EventHandler GMailChecked;
		public event EventHandler GMailChecking;
		public event EventHandler<GMailErrorArgs> GMailFailed;
		
		public GMailState State { get; protected set; }

		public int UnreadCount { get; protected set; }
		public int NewCount { get; protected set; }
		
		public bool HasUnread {
			get { return UnreadCount > 0 && State != GMailState.Error; }
		}

		bool IsChecking { get; set; }
		
		public GMailAtom (string label)
		{
			CurrentLabel = label;
			State = GMailState.ManualReload;
			
			ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
			
			DockServices.System.ConnectionStatusChanged += HandleNeedReset;
			ResetNeeded += HandleNeedReset;
		}
		
		Thread checkerThread;
		
		void HandleNeedReset (object o, EventArgs state)
		{
			ResetTimer ();
		}
		
		List<UnreadMessage> messages = new List<UnreadMessage> ();
		
		public IEnumerable<UnreadMessage> Messages {
			get { return messages as IEnumerable<UnreadMessage>; }
		}
		
		uint UpdateTimer { get; set; }
		
		public void ResetTimer (bool manual)
		{
			if (manual)
				State = GMailState.ManualReload;
			ResetTimer ();
		}
		
		public void StopTimer ()
		{
			if (UpdateTimer > 0)
				GLib.Source.Remove (UpdateTimer);
			UpdateTimer = 0;
			
			if (checkerThread != null)
				checkerThread.Abort ();
		}
			
		public void ResetTimer ()
		{
			StopTimer ();
			
			CheckGMail ();
			
			UpdateTimer = GLib.Timeout.Add (GMailPreferences.RefreshRate * 60 * 1000, () => { 
				CheckGMail (); 
				return true; 
			});
		}
		
		public string CurrentLabel { get; protected set; }
		
		public static bool ValidateCredentials (string username, string password)
		{
			try {
				String[] login = username.Split (new char[] { '@' });
				string domain = login.Length > 1 ? login[1] : "gmail.com";
				string url = "https://mail.google.com/a/" + domain;
				if (domain.Equals ("gmail.com") || domain.Equals ("googlemail.com"))
					url = "https://mail.google.com/mail";
				url += "/feed/atom/";
				
				Log<GMailAtom>.Info ("Fetching Atom feed: " + url);
				HttpWebRequest request = (HttpWebRequest) WebRequest.Create (url);
				request.Timeout = 60000;
				request.UserAgent = DockServices.System.UserAgent;
				request.Credentials = new NetworkCredential (username, password);
				if (DockServices.System.UseProxy)
					request.Proxy = DockServices.System.Proxy;
				
				using (HttpWebResponse response = (HttpWebResponse)request.GetResponse ())
					try { } finally {
						response.Close ();
					}
			} catch (WebException e) {
				if (e.Message.IndexOf ("401") != -1) return false;
			} catch (Exception) { }
			
			return true;
		}
		
		void CheckGMail ()
		{
			if (IsChecking || !DockServices.System.NetworkConnected)
				return;
			
			string password = GMailPreferences.Password;
			if (string.IsNullOrEmpty (GMailPreferences.User) || string.IsNullOrEmpty (password)) {
				OnGMailFailed (Catalog.GetString ("Click to set username and password."));
				return;
			}
			
			IsChecking = true;
			
			checkerThread = DockServices.System.RunOnThread (() => {
				try {
					Gtk.Application.Invoke (delegate { OnGMailChecking (); });

					String[] login = GMailPreferences.User.Split (new char[] { '@' });
					string domain = login.Length > 1 ? login[1] : "gmail.com";
					string url = "https://mail.google.com/a/" + domain;
					if (domain.Equals ("gmail.com") || domain.Equals ("googlemail.com"))
						url = "https://mail.google.com/mail";
					// GMail's atom feed prefers to encode labels using '-'
					url += "/feed/atom/" + HttpUtility.UrlEncode (string.Join ("-", CurrentLabel.Split (new char[]{'/', ' '})));
					
					Log<GMailAtom>.Info ("Fetching Atom feed: " + url);
					HttpWebRequest request = (HttpWebRequest) WebRequest.Create (url);
					request.Timeout = 60000;
					request.UserAgent = DockServices.System.UserAgent;
					request.Credentials = new NetworkCredential (GMailPreferences.User, password);
					if (DockServices.System.UseProxy)
						request.Proxy = DockServices.System.Proxy;
					
					XmlDocument xml = new XmlDocument ();
					XmlNamespaceManager nsmgr = new XmlNamespaceManager (xml.NameTable);
					nsmgr.AddNamespace ("atom", "http://purl.org/atom/ns#");
					
					using (HttpWebResponse response = (HttpWebResponse)request.GetResponse ())
						try {
							xml.Load (response.GetResponseStream ());
						} finally {
							response.Close ();
						}
					
					List<UnreadMessage> tmp = new List<UnreadMessage> ();
					XmlNodeList nodelist = xml.SelectNodes ("//atom:entry", nsmgr);
					
					for (int i = 0; i < nodelist.Count; i++)
					{
						XmlNode item = nodelist.Item (i);
						
						UnreadMessage message = new UnreadMessage ();
						message.Topic = HttpUtility.HtmlDecode (item.SelectSingleNode ("atom:title", nsmgr).InnerText);
						XmlNode from = item.SelectSingleNode ("atom:author/atom:name", nsmgr);
						if (from != null)
							message.FromName = HttpUtility.HtmlDecode (from.InnerText);
						message.FromEmail = item.SelectSingleNode ("atom:author/atom:email", nsmgr).InnerText;
						message.From = message.FromName + " <" + message.FromEmail + ">";
						try {
							message.SendDate = DateTime.Parse (item.SelectSingleNode ("atom:modified", nsmgr).InnerText);
						} catch (Exception) {}
						message.Link = item.SelectSingleNode ("atom:link", nsmgr).Attributes ["href"].InnerText;
						if (message.Topic.Length == 0)
							message.Topic = Catalog.GetString ("(no subject)");
						
						tmp.Add (message);
					}
					
					UnreadCount = Convert.ToInt32 (xml.SelectSingleNode ("//atom:fullcount", nsmgr).InnerText);
					
					NewCount = 0;
					foreach (UnreadMessage message in tmp)
						if (message.SendDate > GMailPreferences.LastChecked)
							NewCount++;
					
					if (GMailPreferences.Notify) {
						if (NewCount > 5)
							Log.Notify (CurrentLabel, "gmail", Catalog.GetString ("You have {0} new, unread messages"), NewCount);
						else
							foreach (UnreadMessage message in tmp)
								if (message.SendDate > GMailPreferences.LastChecked)
									Log.Notify (message.Topic, "gmail", Catalog.GetString ("From: {0}"), message.From);
					}
					
					try {
						GMailPreferences.LastChecked = DateTime.Parse (xml.SelectSingleNode ("/atom:feed/atom:modified", nsmgr).InnerText);
					} catch (Exception) { GMailPreferences.LastChecked = DateTime.Now; }
					
					messages = tmp;
					Gtk.Application.Invoke (delegate { OnGMailChecked (); });
				} catch (ThreadAbortException) {
					Log<GMailAtom>.Debug ("Stoping Atom thread");
				} catch (NullReferenceException e) {
					Log<GMailAtom>.Debug (e.ToString ());
					Gtk.Application.Invoke (delegate {
						OnGMailFailed (Catalog.GetString ("Feed Error"));
					});
				} catch (XmlException e) {
					Log<GMailAtom>.Debug (e.ToString ());
					Gtk.Application.Invoke (delegate {
						OnGMailFailed (Catalog.GetString ("Feed Error"));
					});
				} catch (WebException e) {
					if (e.Message.IndexOf ("401") != -1)
						Gtk.Application.Invoke (delegate {
							OnGMailFailed (Catalog.GetString ("Invalid Username"));
						});
					else
						Gtk.Application.Invoke (delegate {
							OnGMailFailed (Catalog.GetString ("Network Error"));
						});
				} catch (ObjectDisposedException) {
					Gtk.Application.Invoke (delegate {
						OnGMailFailed (Catalog.GetString ("Network Error"));
					});
				} catch (Exception e) {
					Log<GMailAtom>.Error (e.ToString ());
					Gtk.Application.Invoke (delegate {
						OnGMailFailed (Catalog.GetString ("General Error"));
					});
				} finally {
					IsChecking = false;
				}
			});
		}
		
		void OnGMailChecked ()
		{
			State = GMailState.Normal;
			if (GMailChecked != null)
				GMailChecked (null, EventArgs.Empty);
		}
		
		void OnGMailChecking ()
		{
			if (State != GMailState.ManualReload)
				State = GMailState.Reloading;
			if (GMailChecking != null)
				GMailChecking (null, EventArgs.Empty);
		}
		
		void OnGMailFailed (string error)
		{
			State = GMailState.Error;
			if (GMailFailed != null)
				GMailFailed (null, new GMailErrorArgs (error));
		}
		
		public void Dispose ()
		{
			StopTimer ();
			DockServices.System.ConnectionStatusChanged -= HandleNeedReset;
			ResetNeeded -= HandleNeedReset;
		}
	}
}
