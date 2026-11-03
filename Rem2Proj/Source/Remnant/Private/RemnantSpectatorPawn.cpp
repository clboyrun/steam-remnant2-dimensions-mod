#include "RemnantSpectatorPawn.h"

ARemnantSpectatorPawn::ARemnantSpectatorPawn(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->CurrentSpectatorTarget = NULL;
    this->PrevViewTarget = NULL;
}


void ARemnantSpectatorPawn::OnTargetUnavailable(USpectatorTargetComponent* SpectatorTarget) {
}

void ARemnantSpectatorPawn::OnSpectateTarget(AActor* Target) {
}

void ARemnantSpectatorPawn::OnLeaveTarget() {
}



bool ARemnantSpectatorPawn::IsValidTarget(AActor* Target) const {
    return false;
}

int32 ARemnantSpectatorPawn::GetNumAvailableTargets() const {
    return 0;
}

AActor* ARemnantSpectatorPawn::GetCurrentTarget(bool bOnlyIfValid) const {
    return NULL;
}


