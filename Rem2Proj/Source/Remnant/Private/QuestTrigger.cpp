#include "QuestTrigger.h"

AQuestTrigger::AQuestTrigger(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->TriggerVolume = NULL;
    this->bUseManualCollision = false;
}

bool AQuestTrigger::IsLocalPlayerInRegion() const {
    return false;
}

ATriggerVolume* AQuestTrigger::GetTriggerVolume() const {
    return NULL;
}

TArray<ARemnantCharacter*> AQuestTrigger::GetCharacters() {
    return TArray<ARemnantCharacter*>();
}

void AQuestTrigger::EvaluateOverlaps() {
}


