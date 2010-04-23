﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web;

using ManagedFusion.Serialization;
using System.IO;

namespace ManagedFusion.Web.Mvc
{
	/// <summary>
	/// 
	/// </summary>
	public abstract class SerializedResult : ActionResult, IView
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ServiceResult"/> class.
		/// </summary>
		public SerializedResult()
		{
			ContentEncoding = Encoding.UTF8;
			ContentType = "text/xml";

			SerializePublicMembers = true;
			FollowFrameworkIgnoreAttributes = true;

			SerializedHeader = new Dictionary<string, object>();
		}

		/// <summary>
		/// Gets or sets the content encoding.
		/// </summary>
		/// <value>The content encoding.</value>
		public Encoding ContentEncoding
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the type of the content.
		/// </summary>
		/// <value>The type of the content.</value>
		public string ContentType
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether [serialize public members].
		/// </summary>
		/// <value>
		/// 	<see langword="true"/> if [serialize public members]; otherwise, <see langword="false"/>.
		/// </value>
		public bool SerializePublicMembers
		{
			get;
			set;
		}

		/// <summary>
		/// 
		/// </summary>
		public bool FollowFrameworkIgnoreAttributes
		{
			get;
			set;
		}

		/// <summary>
		/// 
		/// </summary>
		public IDictionary<string, object> SerializedHeader
		{
			get;
			internal set;
		}

		/// <summary>
		/// Builds the response.
		/// </summary>
		/// <param name="content">The content.</param>
		/// <returns></returns>
		protected IDictionary<string, object> BuildResponse(object serializableObject, IDictionary<string, object> serializedContent)
		{
			// create body of the response
			IDictionary<string, object> response = new Dictionary<string, object>();
			response.Add("timestamp", DateTime.UtcNow);

			// add serialization headers to the response
			foreach (var header in SerializedHeader)
				response.Add(header.Key, header.Value);

			// check for regular collection
			if (serializableObject is ICollection)
			{
				response.Add("count", ((ICollection)serializableObject).Count);

				if (serializedContent.Count > 1)
					response.Add("collection", serializedContent);
				else
					foreach (var value in serializedContent)
						response.Add(value.Key, value.Value);
			}
			else if (serializedContent.Count > 1)
				response.Add("object", serializedContent);
			else
				foreach (var value in serializedContent)
					response.Add(value.Key, value.Value);

			return response;
		}

		/// <summary>
		/// Gets the content.
		/// </summary>
		protected internal abstract string GetContent();

		/// <summary>
		/// 
		/// </summary>
		protected internal abstract string ContentFileExtension { get; }

		/// <summary>
		/// Gets or sets the data.
		/// </summary>
		/// <value>The data.</value>
		public object Model
		{
			get;
			set;
		}

		/// <summary>
		/// Executes the result.
		/// </summary>
		/// <param name="context">The context.</param>
		public override void ExecuteResult(ControllerContext context)
		{
			if (context == null)
			{
				throw new ArgumentNullException("context");
			}

			string action = context.RouteData.GetRequiredString("action");
			HttpRequestBase request = context.HttpContext.Request;
			HttpResponseBase response = context.HttpContext.Response;
			response.ClearHeaders();
			response.ClearContent();

			if (!String.IsNullOrEmpty(ContentType))
				response.ContentType = ContentType;

			if (ContentEncoding != null)
				response.ContentEncoding = ContentEncoding;

			response.Cache.SetExpires(DateTime.Today.AddDays(-1D));
			response.AppendHeader("X-Robots-Tag", "noindex, follow, noarchive, nosnippet");
			response.AppendHeader("Content-Disposition", String.Format("inline; filename={0}.{1}; creation-date={2:r}", action, ContentFileExtension, DateTime.UtcNow));

			if (!request.IsSecureConnection)
			{
				response.Cache.SetCacheability(HttpCacheability.NoCache);
				response.AppendHeader("Pragma", "no-cache");
				response.AppendHeader("Cache-Control", "private, no-cache, must-revalidate, no-store, pre-check=0, post-check=0, max-stale=0");
			}

			if (Model != null)
			{
				string content = GetContent();

				if (content != null)
				{
					response.AppendHeader("Content-Length", content.Length.ToString());
					response.Write(content);
				}
			}

			response.End();
		}

		#region IView Members

		public void Render(ViewContext viewContext, TextWriter writer)
		{
			Model = viewContext.ViewData.Model;
			ExecuteResult(viewContext);
		}

		#endregion
	}
}