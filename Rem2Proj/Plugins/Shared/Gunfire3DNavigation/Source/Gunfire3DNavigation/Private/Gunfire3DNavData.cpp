#include "Gunfire3DNavData.h"
#include "NavigationData.h"

AGunfire3DNavData::AGunfire3DNavData(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->RuntimeGeneration = ERuntimeGenerationType::Dynamic;
    this->bDrawShell = true;
    this->bDrawOctree = false;
    this->DrawType = ENav3DDrawType::Blocked;
    this->bIncludeVoxelAreas = false;
    this->bDrawSingleLayer = false;
    this->DrawLayerIndex = 0;
    this->VoxelSize = 48.00f;
    this->TileLayerIndex = 3;
    this->TilePoolSize = 4096;
    this->bFixedTilePoolSize = false;
    this->MaxTileGenerationJobs = 1024;
    this->MaxTilesPerGenerationJob = 1;
    this->bDoAsyncGeometryGathering = false;
    this->DefaultMaxSearchNodes = 2048;
    this->DefaultHeuristicScale = 2.00f;
    this->DefaultBaseTraversalCost = 1.00f;
}


