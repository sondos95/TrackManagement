import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { NgModule, provideZonelessChangeDetection } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { FooterComponent } from './components/footer/footer.component';
import { HeaderComponent } from './components/header/header.component';
import { HomeComponent } from './components/home/home.component';
import { DspDistributionTableComponent } from './components/track-detail/dsp-distribution-table.component';
import { TrackDetailPageComponent } from './components/track-detail/track-detail-page.component';
import { TrackListPageComponent } from './components/track-list/track-list-page.component';
import { TrackStatusFilterComponent } from './components/track-list/track-status-filter.component';
import { TrackTableComponent } from './components/track-list/track-table.component';
import { jwtInterceptor } from './interceptors/jwt.interceptor';

@NgModule({
  declarations: [
    AppComponent,
    HeaderComponent,
    FooterComponent,
    HomeComponent,
    TrackListPageComponent,
    TrackDetailPageComponent,
    TrackStatusFilterComponent,
    TrackTableComponent,
    DspDistributionTableComponent,
  ],
  imports: [BrowserModule, AppRoutingModule],
  providers: [
    provideZonelessChangeDetection(),
    provideHttpClient(withInterceptors([jwtInterceptor])),
  ],
  bootstrap: [AppComponent],
})
export class AppModule {}
