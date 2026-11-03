#include "RemnantCursorPanel.h"

URemnantCursorPanel::URemnantCursorPanel() {
    this->CursorPanelType = ECursorPanelType::Gravity;
    this->GravitySpeedMod = 0.10f;
    this->OverrideGravityDistanceNormalized = -1.00f;
    this->FrictionSpeedMod = 0.50f;
    this->CachedLastHoveredTarget = NULL;
}

void URemnantCursorPanel::DeregisterCursorTarget(URemnantCursorTarget* Target) {
}


