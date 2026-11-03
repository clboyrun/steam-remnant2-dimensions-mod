#include "RemnantAnalogCursorManager.h"

URemnantAnalogCursorManager::URemnantAnalogCursorManager(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->CachedSnapWidget = NULL;
}

void URemnantAnalogCursorManager::UpdateCachedCursorLockMode() {
}

void URemnantAnalogCursorManager::SnapToWidget(UUserWidget* WidgetToSnap, bool bUseCursorTarget, const FVector2D& OverrideSnapPoint) {
}

URemnantAnalogCursorManager* URemnantAnalogCursorManager::GetInstance(const UObject* WorldContextObject) {
    return NULL;
}


