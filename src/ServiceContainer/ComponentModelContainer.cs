using System.ComponentModel.Design;
using BepInEx.Logging;

namespace MinishootRandomizer;

public class ComponentModelContainer : IServiceContainer, IBuildable
{
    private readonly ServiceContainer _serviceContainer = new ServiceContainer();
    // We need to store the plugin logger to create a proper ILogger instance.
    private readonly ManualLogSource _pluginLogger;
    private bool _isBuilt = false;

    public ComponentModelContainer(ManualLogSource logger)
    {
        _pluginLogger = logger;
    }

    public void Build()
    {
        _serviceContainer.AddService(typeof(ILogger), new BepInExLogger(_pluginLogger));
        
        _serviceContainer.AddService(typeof(GameEventDispatcher), new GameEventDispatcher(
            (ILogger)_serviceContainer.GetService(typeof(ILogger))
        ));

        _serviceContainer.AddService(typeof(IEnvelopeStorage), new InMemoryEnvelopeStorage());

        _serviceContainer.AddService(typeof(IMessageProcessor), new MessageProcessor());

        _serviceContainer.AddService(typeof(IMessageConsumer), new CoreMessageConsumer(
            (IEnvelopeStorage)_serviceContainer.GetService(typeof(IEnvelopeStorage)),
            (IMessageProcessor)_serviceContainer.GetService(typeof(IMessageProcessor)),
            (ILogger)_serviceContainer.GetService(typeof(ILogger))
        ));

        _serviceContainer.AddService(typeof(CoreMessageDispatcher), new CoreMessageDispatcher(
            (IEnvelopeStorage)_serviceContainer.GetService(typeof(IEnvelopeStorage))
        ));
        _serviceContainer.AddService(typeof(EventMessageDispatcher), new EventMessageDispatcher(
            (CoreMessageDispatcher)_serviceContainer.GetService(typeof(CoreMessageDispatcher))
        ));
        _serviceContainer.AddService(typeof(IMessageDispatcher), _serviceContainer.GetService(typeof(EventMessageDispatcher)));

        _serviceContainer.AddService(typeof(UnityObjectFinder), new UnityObjectFinder());
        // This service is not finished yet, so we will not add it to the container.
        // _serviceContainer.AddService(typeof(CacheableObjectFinder), new CacheableObjectFinder(
        //     (IObjectFinder)_serviceContainer.GetService(typeof(UnityObjectFinder))
        // ));
        _serviceContainer.AddService(typeof(IObjectFinder), _serviceContainer.GetService(typeof(UnityObjectFinder)));

        _serviceContainer.AddService(typeof(ILocationFactory), new DictionaryLocationFactory());

        _serviceContainer.AddService(typeof(IZoneFactory), new DictionaryZoneFactory(
            (ILogger)_serviceContainer.GetService(typeof(ILogger))
        ));

        _serviceContainer.AddService(typeof(ILocationRepository), new CsvLocationRepository(
            "MinishootRandomizer.Resources.locations.csv",
            (ILocationFactory)_serviceContainer.GetService(typeof(ILocationFactory)),
            (ILogger)_serviceContainer.GetService(typeof(ILogger))
        ));

        _serviceContainer.AddService(typeof(IItemFactory), new DictionaryItemFactory(
            (ILogger)_serviceContainer.GetService(typeof(ILogger))
        ));

        _serviceContainer.AddService(typeof(IItemRepository), new CsvItemRepository(
            (IItemFactory)_serviceContainer.GetService(typeof(IItemFactory)),
            "MinishootRandomizer.Resources.items.csv"
        ));

        _serviceContainer.AddService(typeof(IZoneRepository), new CsvZoneRepository(
            "MinishootRandomizer.Resources.zones.csv",
            (IZoneFactory)_serviceContainer.GetService(typeof(IZoneFactory))
        ));

        _serviceContainer.AddService(typeof(IRegionRepository), new CsvRegionRepository(
            "MinishootRandomizer.Resources.regions.csv"
        ));

        _serviceContainer.AddService(typeof(ICloningPassChain), new CloningPassChain());
        ((ICloningPassChain)_serviceContainer.GetService(typeof(ICloningPassChain))).AddPass(new PickupPass());
        ((ICloningPassChain)_serviceContainer.GetService(typeof(ICloningPassChain))).AddPass(new ColliderPass());
        ((ICloningPassChain)_serviceContainer.GetService(typeof(ICloningPassChain))).AddPass(new ChildrenPass());
        ((ICloningPassChain)_serviceContainer.GetService(typeof(ICloningPassChain))).AddPass(new MetaCloningPass());

        _serviceContainer.AddService(typeof(CloneBasedFactory), new CloneBasedFactory(
            (IObjectFinder)_serviceContainer.GetService(typeof(IObjectFinder)),
            (ICloningPassChain)_serviceContainer.GetService(typeof(ICloningPassChain)),
            (ILogger)_serviceContainer.GetService(typeof(ILogger))
        ));

        _serviceContainer.AddService(typeof(ITranslator), new NullTranslator());

        _serviceContainer.AddService(typeof(PrefabSpriteProvider), new PrefabSpriteProvider(
            (IObjectFinder)_serviceContainer.GetService(typeof(IObjectFinder)),
            (ILogger)_serviceContainer.GetService(typeof(ILogger))
        ));
        _serviceContainer.AddService(typeof(ISpriteProvider), new ArchipelagoSpriteProvider(
            (PrefabSpriteProvider)_serviceContainer.GetService(typeof(PrefabSpriteProvider))
        ));

        _serviceContainer.AddService(typeof(IItemPresentationProvider), new CoreItemPresentationProvider(
            (ISpriteProvider)_serviceContainer.GetService(typeof(ISpriteProvider))
        ));

        _serviceContainer.AddService(typeof(IGameObjectFactory), new SpriteBasedFactory(
            (CloneBasedFactory)_serviceContainer.GetService(typeof(CloneBasedFactory)),
            (IItemPresentationProvider)_serviceContainer.GetService(typeof(IItemPresentationProvider)),
            (ILogger)_serviceContainer.GetService(typeof(ILogger))
        ));

        _serviceContainer.AddService(typeof(PickupManager), new PickupManager());
        ((GameEventDispatcher)_serviceContainer.GetService(typeof(GameEventDispatcher))).LoadingSaveFile
            += ((PickupManager)_serviceContainer.GetService(typeof(PickupManager))).OnLoadingSaveFile;
        ((GameEventDispatcher)_serviceContainer.GetService(typeof(GameEventDispatcher))).NpcFreed
            += ((PickupManager)_serviceContainer.GetService(typeof(PickupManager))).OnNpcFreed;
        ((GameEventDispatcher)_serviceContainer.GetService(typeof(GameEventDispatcher))).PlayerStatsChanged
            += ((PickupManager)_serviceContainer.GetService(typeof(PickupManager))).OnPlayerStatsChanged;
        ((GameEventDispatcher)_serviceContainer.GetService(typeof(GameEventDispatcher))).EnteringEncounter
            += ((PickupManager)_serviceContainer.GetService(typeof(PickupManager))).OnEnteringEncounter;
        ((GameEventDispatcher)_serviceContainer.GetService(typeof(GameEventDispatcher))).ExitingEncounter
            += ((PickupManager)_serviceContainer.GetService(typeof(PickupManager))).OnExitingEncounter;

        _serviceContainer.AddService(typeof(ILocationVisitor), new GameObjectCreationVisitor(
            (IGameObjectFactory)_serviceContainer.GetService(typeof(IGameObjectFactory)),
            (IObjectFinder)_serviceContainer.GetService(typeof(IObjectFinder)),
            (PickupManager)_serviceContainer.GetService(typeof(PickupManager)),
            (ILogger)_serviceContainer.GetService(typeof(ILogger))
        ));

        _serviceContainer.AddService(typeof(IProgressionStorage), new WorldStateProgressionStorage());

        _serviceContainer.AddService(typeof(IRandomizerContextProvider), new ImguiContextProvider(
            (IObjectFinder)_serviceContainer.GetService(typeof(IObjectFinder)),
            (ILogger)_serviceContainer.GetService(typeof(ILogger))
        ));

        _serviceContainer.AddService(typeof(IItemCounter), new PlayerStateItemCounter());

        _serviceContainer.AddService(typeof(IArchipelagoClient), new MultiClient(
            (IItemCounter)_serviceContainer.GetService(typeof(IItemCounter)),
            (ILogger)_serviceContainer.GetService(typeof(ILogger))
        ));

        _serviceContainer.AddService(typeof(ArchipelagoRandomizerEngine), new ArchipelagoRandomizerEngine(
            (IArchipelagoClient)_serviceContainer.GetService(typeof(IArchipelagoClient)),
            (IItemRepository)_serviceContainer.GetService(typeof(IItemRepository)),
            (ILocationRepository)_serviceContainer.GetService(typeof(ILocationRepository)),
            (IProgressionStorage)_serviceContainer.GetService(typeof(IProgressionStorage)),
            (IMessageDispatcher)_serviceContainer.GetService(typeof(IMessageDispatcher)),
            (ILogger)_serviceContainer.GetService(typeof(ILogger))
        ));
        ((MultiClient)_serviceContainer.GetService(typeof(IArchipelagoClient))).ItemReceived
            += ((ArchipelagoRandomizerEngine)_serviceContainer.GetService(typeof(ArchipelagoRandomizerEngine))).OnItemReceived;

        _serviceContainer.AddService(typeof(VanillaRandomizerEngine), new VanillaRandomizerEngine());

        _serviceContainer.AddService(typeof(IRandomizerEngine), new ContextualRandomizerEngine(
            (ArchipelagoRandomizerEngine)_serviceContainer.GetService(typeof(ArchipelagoRandomizerEngine)),
            (VanillaRandomizerEngine)_serviceContainer.GetService(typeof(VanillaRandomizerEngine)),
            (IRandomizerContextProvider)_serviceContainer.GetService(typeof(IRandomizerContextProvider))
        ));
        
        _serviceContainer.AddService(typeof(RandomizerEngineManager), new RandomizerEngineManager(
            (IRandomizerEngine)_serviceContainer.GetService(typeof(IRandomizerEngine))
        ));
        ((GameEventDispatcher)_serviceContainer.GetService(typeof(GameEventDispatcher))).LoadingSaveFile
            += ((RandomizerEngineManager)_serviceContainer.GetService(typeof(RandomizerEngineManager))).OnLoadingSaveFile;
        ((GameEventDispatcher)_serviceContainer.GetService(typeof(GameEventDispatcher))).ExitingGame
            += ((RandomizerEngineManager)_serviceContainer.GetService(typeof(RandomizerEngineManager))).OnExitingGame;

        _serviceContainer.AddService(typeof(SendCheckedLocationsHandler), new SendCheckedLocationsHandler(
            (IArchipelagoClient)_serviceContainer.GetService(typeof(IArchipelagoClient))
        ));
        ((CoreMessageConsumer)_serviceContainer.GetService(typeof(IMessageConsumer))).AddHandler<SendCheckedLocationsMessage>(
            (SendCheckedLocationsHandler)_serviceContainer.GetService(typeof(SendCheckedLocationsHandler))
        );

        _serviceContainer.AddService(typeof(SendGoalHandler), new SendGoalHandler(
            (IArchipelagoClient)_serviceContainer.GetService(typeof(IArchipelagoClient))
        ));
        ((CoreMessageConsumer)_serviceContainer.GetService(typeof(IMessageConsumer))).AddHandler<SendGoalMessage>(
            (SendGoalHandler)_serviceContainer.GetService(typeof(SendGoalHandler))
        );

        _serviceContainer.AddService(typeof(ReceiveItemHandler), new ReceiveItemHandler());
        ((CoreMessageConsumer)_serviceContainer.GetService(typeof(IMessageConsumer))).AddHandler<ReceiveItemMessage>(
            (ReceiveItemHandler)_serviceContainer.GetService(typeof(ReceiveItemHandler))
        );

        _serviceContainer.AddService(typeof(IPrefabCollector), new ChainPrefabCollector());
        ((ChainPrefabCollector)_serviceContainer.GetService(typeof(IPrefabCollector))).AddCollector(
            (IPrefabCollector)_serviceContainer.GetService(typeof(CloneBasedFactory))
        );
        ((ChainPrefabCollector)_serviceContainer.GetService(typeof(IPrefabCollector))).AddCollector(
            (IPrefabCollector)_serviceContainer.GetService(typeof(PrefabSpriteProvider))
        );

        // Patchers
        _serviceContainer.AddService(typeof(ShopReplacementPatcher), new ShopReplacementPatcher(
            (IRandomizerEngine)_serviceContainer.GetService(typeof(IRandomizerEngine)),
            (IObjectFinder)_serviceContainer.GetService(typeof(IObjectFinder)),
            (ILogger)_serviceContainer.GetService(typeof(ILogger))
        ));
        ((GameEventDispatcher)_serviceContainer.GetService(typeof(GameEventDispatcher))).EnteringGameLocation
            += ((ShopReplacementPatcher)_serviceContainer.GetService(typeof(ShopReplacementPatcher))).OnEnteringGameLocation;
        ((GameEventDispatcher)_serviceContainer.GetService(typeof(GameEventDispatcher))).ExitingGame
            += ((ShopReplacementPatcher)_serviceContainer.GetService(typeof(ShopReplacementPatcher))).OnExitingGame;

        _serviceContainer.AddService(typeof(ItemReplacementPatcher), new ItemReplacementPatcher(
            (IRandomizerEngine)_serviceContainer.GetService(typeof(IRandomizerEngine)),
            (ILocationVisitor)_serviceContainer.GetService(typeof(ILocationVisitor)),
            (IZoneRepository)_serviceContainer.GetService(typeof(IZoneRepository)),
            (IRegionRepository)_serviceContainer.GetService(typeof(IRegionRepository)),
            (ILocationRepository)_serviceContainer.GetService(typeof(ILocationRepository)),
            (ILogger)_serviceContainer.GetService(typeof(ILogger))
        ));
        ((GameEventDispatcher)_serviceContainer.GetService(typeof(GameEventDispatcher))).EnteringGameLocation
            += ((ItemReplacementPatcher)_serviceContainer.GetService(typeof(ItemReplacementPatcher))).OnEnteringGameLocation;
        ((GameEventDispatcher)_serviceContainer.GetService(typeof(GameEventDispatcher))).ExitingGame
            += ((ItemReplacementPatcher)_serviceContainer.GetService(typeof(ItemReplacementPatcher))).OnExitingGame;

        _serviceContainer.AddService(typeof(XpCrystalRemovalPatcher), new XpCrystalRemovalPatcher(
            (IRandomizerEngine)_serviceContainer.GetService(typeof(IRandomizerEngine)),
            (IObjectFinder)_serviceContainer.GetService(typeof(IObjectFinder)),
            (ILogger)_serviceContainer.GetService(typeof(ILogger))
        ));
        ((GameEventDispatcher)_serviceContainer.GetService(typeof(GameEventDispatcher))).EnteringGameLocation
            += ((XpCrystalRemovalPatcher)_serviceContainer.GetService(typeof(XpCrystalRemovalPatcher))).OnEnteringGameLocation;
        ((GameEventDispatcher)_serviceContainer.GetService(typeof(GameEventDispatcher))).ExitingGame
            += ((XpCrystalRemovalPatcher)_serviceContainer.GetService(typeof(XpCrystalRemovalPatcher))).OnExitingGame;

        _serviceContainer.AddService(typeof(BlockedForestPatcher), new BlockedForestPatcher(
            (IRandomizerEngine)_serviceContainer.GetService(typeof(IRandomizerEngine)),
            (IObjectFinder)_serviceContainer.GetService(typeof(IObjectFinder)),
            (IGameObjectFactory)_serviceContainer.GetService(typeof(IGameObjectFactory)),
            (ILogger)_serviceContainer.GetService(typeof(ILogger))
        ));
        ((GameEventDispatcher)_serviceContainer.GetService(typeof(GameEventDispatcher))).EnteringGameLocation
            += ((BlockedForestPatcher)_serviceContainer.GetService(typeof(BlockedForestPatcher))).OnEnteringGameLocation;
        ((GameEventDispatcher)_serviceContainer.GetService(typeof(GameEventDispatcher))).ExitingGame
            += ((BlockedForestPatcher)_serviceContainer.GetService(typeof(BlockedForestPatcher))).OnExitingGame;

        _serviceContainer.AddService(typeof(SimpleTempleExitPatcher), new SimpleTempleExitPatcher(
            (IRandomizerEngine)_serviceContainer.GetService(typeof(IRandomizerEngine)),
            (IObjectFinder)_serviceContainer.GetService(typeof(IObjectFinder)),
            (ILogger)_serviceContainer.GetService(typeof(ILogger))
        ));
        ((GameEventDispatcher)_serviceContainer.GetService(typeof(GameEventDispatcher))).EnteringGameLocation
            += ((SimpleTempleExitPatcher)_serviceContainer.GetService(typeof(SimpleTempleExitPatcher))).OnEnteringGameLocation;
        ((GameEventDispatcher)_serviceContainer.GetService(typeof(GameEventDispatcher))).ExitingGame
            += ((SimpleTempleExitPatcher)_serviceContainer.GetService(typeof(SimpleTempleExitPatcher))).OnExitingGame;

        _serviceContainer.AddService(typeof(FamilyReunitedPatcher), new FamilyReunitedPatcher(
            (IRandomizerEngine)_serviceContainer.GetService(typeof(IRandomizerEngine)),
            (IObjectFinder)_serviceContainer.GetService(typeof(IObjectFinder)),
            (ILogger)_serviceContainer.GetService(typeof(ILogger))
        ));
        ((GameEventDispatcher)_serviceContainer.GetService(typeof(GameEventDispatcher))).EnteringGameLocation
            += ((FamilyReunitedPatcher)_serviceContainer.GetService(typeof(FamilyReunitedPatcher))).OnEnteringGameLocation;
        ((GameEventDispatcher)_serviceContainer.GetService(typeof(GameEventDispatcher))).ExitingGame
            += ((FamilyReunitedPatcher)_serviceContainer.GetService(typeof(FamilyReunitedPatcher))).OnExitingGame;

        _serviceContainer.AddService(typeof(BossPatcher), new BossPatcher(
            (IRandomizerEngine)_serviceContainer.GetService(typeof(IRandomizerEngine)),
            (IObjectFinder)_serviceContainer.GetService(typeof(IObjectFinder)),
            (ILogger)_serviceContainer.GetService(typeof(ILogger))
        ));
        ((GameEventDispatcher)_serviceContainer.GetService(typeof(GameEventDispatcher))).EnteringGameLocation
            += ((BossPatcher)_serviceContainer.GetService(typeof(BossPatcher))).OnEnteringGameLocation;
        ((GameEventDispatcher)_serviceContainer.GetService(typeof(GameEventDispatcher))).ExitingGame
            += ((BossPatcher)_serviceContainer.GetService(typeof(BossPatcher))).OnExitingGame;

        _isBuilt = true;
    }

    public T Get<T>()
    {
        if (!_isBuilt)
        {
            Build();
        }

        object service = _serviceContainer.GetService(typeof(T));
        if (service == null)
        {
            throw new ServiceNotFoundException($"Service {typeof(T).Name} not found");
        }
        return (T) service;
    }

    public bool Has<T>()
    {
        if (!_isBuilt)
        {
            Build();
        }

        return _serviceContainer.GetService(typeof(T)) != null;
    }
}
