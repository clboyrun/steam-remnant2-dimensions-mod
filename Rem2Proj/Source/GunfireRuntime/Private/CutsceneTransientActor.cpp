#include "CutsceneTransientActor.h"

ACutsceneTransientActor::ACutsceneTransientActor(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->bIsInCutscene = false;
}

void ACutsceneTransientActor::OnEventCinematic(bool Start, const TArray<FName>& CinematicTags) {
}

void ACutsceneTransientActor::OnCinematic_Implementation(bool IsPlaying, const TArray<FName>& CinematicTags) {
}

void ACutsceneTransientActor::ForceCutscene(const TArray<FName>& CinematicTags, bool bIsPlaying) {
}


