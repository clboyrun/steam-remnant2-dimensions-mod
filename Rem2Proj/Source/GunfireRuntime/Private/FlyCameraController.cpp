#include "FlyCameraController.h"

AFlyCameraController::AFlyCameraController(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->ClickEventKeys.AddDefaulted(1);
    this->bShouldPerformFullTickWhenPaused = true;
}


