#include "EnvQueryTest_FieldOfView.h"

UEnvQueryTest_FieldOfView::UEnvQueryTest_FieldOfView() {
    this->TestPurpose = EEnvTestPurpose::Filter;
    this->FilterType = EEnvTestFilterType::Match;
    this->ScoringEquation = EEnvTestScoreEquation::Constant;
    this->ViewFromContext = NULL;
    this->RequireAll = false;
    this->OutOfRangeValue = false;
    this->AllowCrossingFieldOfView = true;
}


