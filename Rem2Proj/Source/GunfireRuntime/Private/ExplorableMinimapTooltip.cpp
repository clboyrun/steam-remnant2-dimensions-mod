#include "ExplorableMinimapTooltip.h"
#include "Components/WidgetComponent.h"

UExplorableMinimapTooltip::UExplorableMinimapTooltip(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->Space = EWidgetSpace::Screen;
    this->TooltipHideDelay = 0.50f;
}


