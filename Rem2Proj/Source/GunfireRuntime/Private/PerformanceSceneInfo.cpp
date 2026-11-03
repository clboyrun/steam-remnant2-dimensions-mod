#include "PerformanceSceneInfo.h"

APerformanceSceneInfo::APerformanceSceneInfo(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->Verbosity = 0;
    this->GroupIdx = 0;
    this->EditorOnlyActorsCount = 0;
}

void APerformanceSceneInfo::ExecutePerfSceneInfo(const TArray<FString>& Args) {
}


