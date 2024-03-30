﻿using System;
using System.Collections.Generic;
using Yafc.Model;
using Yafc.Ui;
using Yafc.Widgets;

namespace Yafc.Workspace {
    public class SummaryView : ProjectPageView<Summary> {
        private class SummaryScrollArea : ScrollArea {
            private static readonly float DefaultHeight = 10;

            public SummaryScrollArea(GuiBuilder builder) : base(DefaultHeight, builder, default, false, true, true) {
            }

            public new void Build(ImGui gui) {
                // Maximize scroll area to fit parent area (minus header and 'show issues' heights, and some (2) padding probably)
                Build(gui, gui.valid ? gui.parent.contentSize.Y - HeaderFont.size - Font.text.size - ScrollbarSize - 2 : DefaultHeight);
            }
        }

        private class SummaryTabColumn : TextDataColumn<ProjectPage> {
            private const float FirstColumnWidth = 14f; // About 20 'o' wide

            public SummaryTabColumn() : base("Tab", FirstColumnWidth) {
            }

            public override void BuildElement(ImGui gui, ProjectPage page) {
                if (page?.contentType != typeof(ProductionTableModel)) {
                    return;
                }

                using (gui.EnterGroup(new Padding(0.5f, 0.2f, 0.2f, 0.5f))) {
                    gui.spacing = 0.2f;
                    if (page.icon != null) {
                        gui.BuildIcon(page.icon.icon);
                    }
                    else {
                        _ = gui.AllocateRect(0f, 1.5f);
                    }

                    gui.BuildText(page.name);
                }
            }
        }

        private class SummaryDataColumn : TextDataColumn<ProjectPage> {
            protected readonly SummaryView view;

            public SummaryDataColumn(SummaryView view) : base("Linked", float.MaxValue) {
                this.view = view;
            }

            public override void BuildElement(ImGui gui, ProjectPage page) {
                if (page?.contentType != typeof(ProductionTableModel)) {
                    return;
                }

                ProductionTableModel table = page.content as ProductionTableModel;
                using var grid = gui.EnterInlineGrid(ElementWidth, ElementSpacing);
                foreach (KeyValuePair<string, GoodDetails> goodInfo in view.allGoods) {
                    if (!view.searchQuery.Match(goodInfo.Key)) {
                        continue;
                    }

                    float amountAvailable = YafcRounding((goodInfo.Value.totalProvided > 0 ? goodInfo.Value.totalProvided : 0) + goodInfo.Value.extraProduced);
                    float amountNeeded = YafcRounding((goodInfo.Value.totalProvided < 0 ? -goodInfo.Value.totalProvided : 0) + goodInfo.Value.totalNeeded);
                    if (view.model.showOnlyIssues && (Math.Abs(amountAvailable - amountNeeded) < Epsilon || amountNeeded == 0)) {
                        continue;
                    }

                    grid.Next();
                    bool enoughProduced = amountAvailable >= amountNeeded;
                    ProductionLink link = table.links.Find(x => x.goods.name == goodInfo.Key);
                    if (link != null) {
                        if (link.amount != 0f) {
                            DrawProvideProduct(gui, link, page, goodInfo.Value, enoughProduced);
                        }
                    }
                    else {
                        if (Array.Exists(table.flow, x => x.goods.name == goodInfo.Key)) {
                            ProductionTableFlow flow = Array.Find(table.flow, x => x.goods.name == goodInfo.Key);
                            if (Math.Abs(flow.amount) > Epsilon) {

                                DrawRequestProduct(gui, flow, enoughProduced);
                            }
                        }
                    }
                }
            }

            private static void DrawProvideProduct(ImGui gui, ProductionLink element, ProjectPage page, GoodDetails goodInfo, bool enoughProduced) {
                gui.allocator = RectAllocator.Stretch;
                gui.spacing = 0f;

                GoodsWithAmountEvent evt = gui.BuildFactorioObjectWithEditableAmount(element.goods, element.amount, element.goods.flowUnitOfMeasure, out float newAmount, (element.amount > 0 && enoughProduced) || (element.amount < 0 && goodInfo.extraProduced == -element.amount) ? SchemeColor.Primary : SchemeColor.Error);
                if (evt == GoodsWithAmountEvent.TextEditing && newAmount != 0) {
                    SetProviderAmount(element, page, newAmount);
                }
                else if (evt == GoodsWithAmountEvent.ButtonClick) {
                    SetProviderAmount(element, page, YafcRounding(goodInfo.sum));
                }
            }
            private static void DrawRequestProduct(ImGui gui, ProductionTableFlow flow, bool enoughProduced) {
                gui.allocator = RectAllocator.Stretch;
                gui.spacing = 0f;
                _ = gui.BuildFactorioObjectWithAmount(flow.goods, -flow.amount, flow.goods?.flowUnitOfMeasure ?? UnitOfMeasure.None, flow.amount > Epsilon ? enoughProduced ? SchemeColor.Green : SchemeColor.Error : SchemeColor.None);
            }

            private static void SetProviderAmount(ProductionLink element, ProjectPage page, float newAmount) {
                element.RecordUndo().amount = newAmount;
                // Hack: Force recalculate the page (and make sure to catch the content change event caused by the recalculation)
                page.SetActive(true);
                page.SetToRecalculate();
                page.SetActive(false);
            }
        }

        private static readonly float Epsilon = 1e-5f;
        private static readonly float ElementWidth = 3;
        private static readonly float ElementSpacing = 1;

        private struct GoodDetails {
            public float totalProvided;
            public float totalNeeded;
            public float extraProduced;
            public float sum;
        }

        private static readonly Font HeaderFont = Font.header;

        private Project project;
        private SearchQuery searchQuery;

        private readonly SummaryScrollArea scrollArea;
        private readonly SummaryDataColumn goodsColumn;
        private readonly DataGrid<ProjectPage> mainGrid;

        private readonly Dictionary<string, GoodDetails> allGoods = new Dictionary<string, GoodDetails>();


        public SummaryView() {
            goodsColumn = new SummaryDataColumn(this);
            TextDataColumn<ProjectPage>[] columns = new TextDataColumn<ProjectPage>[]
            {
                new SummaryTabColumn(),
                goodsColumn,
            };
            scrollArea = new SummaryScrollArea(BuildScrollArea);
            mainGrid = new DataGrid<ProjectPage>(columns);
        }

        public void SetProject(Project project) {
            if (this.project != null) {
                this.project.metaInfoChanged -= Recalculate;
                foreach (ProjectPage page in project.pages) {
                    page.contentChanged -= Recalculate;
                }
            }

            this.project = project;

            project.metaInfoChanged += Recalculate;
            foreach (ProjectPage page in project.pages) {
                page.contentChanged += Recalculate;
            }
        }

        protected override void BuildPageTooltip(ImGui gui, Summary contents) {
        }

        protected override void BuildHeader(ImGui gui) {
            base.BuildHeader(gui);

            gui.allocator = RectAllocator.Center;
            gui.BuildText("Production Sheet Summary", HeaderFont, false, RectAlignment.Middle);
            gui.allocator = RectAllocator.LeftAlign;
        }

        protected override void BuildContent(ImGui gui) {
            if (gui.BuildCheckBox("Only show issues", model.showOnlyIssues, out bool newValue)) {
                model.showOnlyIssues = newValue;
                Recalculate();
            }

            scrollArea.Build(gui);
        }

        private void BuildScrollArea(ImGui gui) {
            foreach (Guid displayPage in project.displayPages) {
                ProjectPage page = project.FindPage(displayPage);
                if (page?.contentType != typeof(ProductionTableModel)) {
                    continue;
                }

                _ = mainGrid.BuildRow(gui, page);
            }
        }

        private void Recalculate() {
            Recalculate(false);
        }

        private void Recalculate(bool visualOnly) {
            allGoods.Clear();
            foreach (Guid displayPage in project.displayPages) {
                ProjectPage page = project.FindPage(displayPage);
                ProductionTableModel content = page?.content as ProductionTableModel;
                if (content == null) {
                    continue;
                }

                foreach (ProductionLink link in content.links) {
                    if (link.amount != 0f) {
                        GoodDetails value = allGoods.GetValueOrDefault(link.goods.name);
                        value.totalProvided += YafcRounding(link.amount); ;
                        allGoods[link.goods.name] = value;
                    }
                }

                foreach (ProductionTableFlow flow in content.flow) {
                    if (flow.amount < -Epsilon) {
                        GoodDetails value = allGoods.GetValueOrDefault(flow.goods.name);
                        value.totalNeeded -= YafcRounding(flow.amount); ;
                        value.sum -= YafcRounding(flow.amount); ;
                        allGoods[flow.goods.name] = value;
                    }
                    else if (flow.amount > Epsilon) {
                        if (!content.links.Exists(x => x.goods == flow.goods)) {
                            // Only count extras if not linked
                            GoodDetails value = allGoods.GetValueOrDefault(flow.goods.name);
                            value.extraProduced += YafcRounding(flow.amount);
                            value.sum -= YafcRounding(flow.amount);
                            allGoods[flow.goods.name] = value;
                        }
                    }
                }
            }

            int count = 0;
            foreach (KeyValuePair<string, GoodDetails> entry in allGoods) {
                float amountAvailable = YafcRounding((entry.Value.totalProvided > 0 ? entry.Value.totalProvided : 0) + entry.Value.extraProduced);
                float amountNeeded = YafcRounding((entry.Value.totalProvided < 0 ? -entry.Value.totalProvided : 0) + entry.Value.totalNeeded);
                if (model != null && model.showOnlyIssues && (Math.Abs(amountAvailable - amountNeeded) < Epsilon || amountNeeded == 0)) {
                    continue;
                }
                count++;
            }

            goodsColumn.width = count * (ElementWidth + ElementSpacing);

            Rebuild(visualOnly);
            scrollArea.RebuildContents();
        }

        // Convert/truncate value as shown in UI to prevent slight mismatches
        private static float YafcRounding(float value) {
            _ = DataUtils.TryParseAmount(DataUtils.FormatAmount(value, UnitOfMeasure.Second), out float result, UnitOfMeasure.Second);
            return result;
        }

        public override void SetSearchQuery(SearchQuery query) {
            searchQuery = query;
            bodyContent.Rebuild();
            scrollArea.Rebuild();
        }

        public override void CreateModelDropdown(ImGui gui, Type type, Project project) {
        }
    }
}
