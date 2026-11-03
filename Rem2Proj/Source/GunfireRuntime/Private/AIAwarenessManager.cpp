#include "AIAwarenessManager.h"

UAIAwarenessManager::UAIAwarenessManager(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->MaxUpdatesPerFrame = 2;
    this->bShouldUpdateAwareness = true;
}

void UAIAwarenessManager::SendAwarenessSignalSegment(EAwarenessSignal Type, AActor* Source, const FVector& From, const FVector& To, float Radius) {
}

void UAIAwarenessManager::SendAwarenessSignal(EAwarenessSignal Type, AActor* Source, const FVector& Position, float Radius, bool UseSourceForLastKnownPosition, bool bApplySightRangeBonus) {
}

UAIAwarenessManager* UAIAwarenessManager::GetInstance(UObject* WorldContextObject) {
    return NULL;
}


