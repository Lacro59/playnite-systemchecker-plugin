using CommonPluginsShared;
using CommonPluginsStores.Epic;
using CommonPluginsStores.Epic.Models.Query;
using CommonPluginsStores.Models;
using Playnite.SDK.Models;
using System;
using System.Linq;
using SystemChecker.Models;

namespace SystemChecker.Clients
{
	/// <summary>
	/// Retrieves system requirements from the Epic Games Store product page.
	/// Never requires user authentication.
	/// <para>
	/// <b>Key constraint:</b> <see cref="Game.GameId"/> = <c>Asset.AppName</c> (e.g. <c>"FallGuys"</c>),
	/// which is neither a namespace nor a store slug.
	/// </para>
	/// <para>
	/// Slug resolution order:
	/// <list type="number">
	///   <item>
	///     <b>Store URL from <see cref="Game.Links"/></b> —
	///     parses any <c>store.epicgames.com/…/p/&lt;slug&gt;</c> link directly.
	///     Zero API calls.
	///   </item>
	///   <item>
	///     <b>Catalog search by game name</b> —
	///     <see cref="EpicApi.SearchStoreByName"/> resolves namespace + slug in one
	///     anonymous GraphQL call (<c>searchStoreQuery</c>).
	///   </item>
	/// </list>
	/// Authentication-dependent methods (<c>GetAssets</c>, <c>GetNamespaceFromGame</c>) are never called.
	/// </para>
	/// </summary>
	public class EpicRequirements : RequirementMetadata
	{
		private readonly EpicApi _epicApi;

		/// <summary>Product slug resolved during <see cref="GetRequirements(Game)"/>.</summary>
		private string _productSlug;

		public EpicRequirements()
		{
			_epicApi = new EpicApi(PluginDatabase.PluginName, PlayniteTools.ExternalPlugin.SystemChecker);
		}

		// -----------------------------------------------------------------------
		// Public entry-point
		// -----------------------------------------------------------------------

		/// <summary>
		/// Resolves system requirements for <paramref name="game"/> via the Epic store.
		/// Does not require authentication.
		/// </summary>
		/// <param name="game">Target game. Must not be <c>null</c>.</param>
		public PluginGameRequirements GetRequirements(Game game)
		{
			Initialize(game);
			_productSlug = ResolveSlug(game);
			return GetRequirements();
		}

		// -----------------------------------------------------------------------
		// RequirementMetadata overrides
		// -----------------------------------------------------------------------

		/// <inheritdoc/>
		/// <remarks>Caller must have invoked <see cref="GetRequirements(Game)"/> first to set context.</remarks>
		public override PluginGameRequirements GetRequirements()
		{
			ResetRequirements();

			if (_productSlug.IsNullOrEmpty())
			{
				Logger.Warn($"EpicRequirements - No product slug found for '{GameContext?.Name}' (GameId: '{GameContext?.GameId}').");
				return PluginGameRequirements;
			}

			return GetRequirements(_productSlug);
		}

		/// <inheritdoc/>
		/// <param name="url">
		/// Epic store product slug (not a full URL). Example: <c>"the-escapists"</c>.
		/// </param>
		public override PluginGameRequirements GetRequirements(string url)
		{
			GameRequirements apiResult = _epicApi.GetGameRequirements(url);
			return BuildRequirementsFromApiResult(apiResult, nameof(EpicRequirements), $"slug: {url}");
		}

		// -----------------------------------------------------------------------
		// Private helpers
		// -----------------------------------------------------------------------

		/// <summary>
		/// Resolves the Epic store product slug for <paramref name="game"/> without any authentication.
		/// <para>
		/// <b>Step 1 — Store URL from links (zero API calls):</b><br/>
		/// Iterates <see cref="Game.Links"/> for a <c>store.epicgames.com/…/p/&lt;slug&gt;</c> URL
		/// and extracts the slug segment directly. This is the fastest path and works for any
		/// library source when the user has a store link saved.
		/// </para>
		/// <para>
		/// <b>Step 2 — Catalog search by name (one anonymous GraphQL call):</b><br/>
		/// Calls <see cref="EpicApi.SearchStoreByName"/> which uses <c>searchStoreQuery</c> to find
		/// the best-matching offer. On a confident match, the <c>Element.UrlSlug</c> /
		/// <c>CatalogNs.Mappings[0].PageSlug</c> is used as the product slug.
		/// </para>
		/// </summary>
		/// <returns>The product slug, or <c>null</c> when all steps fail.</returns>
		private string ResolveSlug(Game game)
		{
			if (game == null)
			{
				return null;
			}

			// Step 1 — extract slug directly from a stored store.epicgames.com link.
			string slugFromLinks = _epicApi.GetUrlSlugFromGameLinks(game);
			if (!slugFromLinks.IsNullOrEmpty())
			{
				Logger.Info($"EpicRequirements.ResolveSlug: Step 1 (Links) → '{slugFromLinks}' for '{game.Name}'.");
				return slugFromLinks;
			}

			// Step 2 — anonymous catalog search by game name.
			SearchStoreResponse searchStoreResponse = _epicApi.QuerySearchStore(game.Name, "games").GetAwaiter().GetResult();
			if (searchStoreResponse?.Data?.Catalog?.SearchStore?.Elements?.Count > 0)
			{
				// Prefer the canonical pageSlug from CatalogNs mappings.
				var element = searchStoreResponse.Data.Catalog.SearchStore.Elements.First();
				string urlSlug = element.UrlSlug;
				if (!urlSlug.IsNullOrEmpty())
				{
					Logger.Info($"EpicRequirements.ResolveSlug: Step 2 (SearchStore) → '{urlSlug}' for '{game.Name}'.");
					return urlSlug;
				}
			}

			Logger.Warn($"EpicRequirements.ResolveSlug: All steps failed for '{game.Name}' (GameId: '{game.GameId}').");
			return null;
		}
	}
}